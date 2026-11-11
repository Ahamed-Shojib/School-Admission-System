// Automated black-box tests: every endpoint, the required fields, status codes and the business rules.
// Run against a running stack:  docker compose run --rm api-tests     (or: cd tests/api && node --test automation.test.mjs)
import assert from 'node:assert/strict';
import { before, describe, it } from 'node:test';
import {
  ADMIN_AUTH_REQUIRED,
  adminToken,
  HOLD_SECONDS,
  apply,
  applyOk,
  assertErrorShape,
  assertSeatMath,
  call,
  cancel,
  confirm,
  createClass,
  getClass,
  getHold,
  myHolds,
  newStudentId,
  sleep,
  waitForApi,
} from './helpers.mjs';

before(waitForApi);

describe('Health', () => {
  it('GET /health answers 200', async () => {
    assert.equal((await call('GET', '/health')).status, 200);
  });
});

describe('POST /classes (create a class)', () => {
  it('returns 201 with id, name and totalSeats', async () => {
    const created = await createClass({ seats: 120, name: 'Class 6 - Morning' });
    assert.match(created.id, /\S+/);
    assert.equal(created.name, 'Class 6 - Morning');
    assert.equal(created.totalSeats, 120);
    assert.equal(created.shift, 'Morning-Shift');
  });

  it('stores the shift in its canonical spelling and allows leaving it out', async () => {
    const day = await createClass({ shift: 'day-shift' });
    assert.equal(day.shift, 'Day-Shift');

    const noShift = await call('POST', '/classes', {
      body: { name: 'No shift class', totalSeats: 5 },
      token: await adminToken(),
    });
    assert.equal(noShift.status, 201);
    assert.equal(noShift.body.shift ?? null, null);
  });

  it('rejects invalid input with 400 and a { code, message } body', async () => {
    const token = await adminToken();
    const post = (body) => call('POST', '/classes', { body, token });

    assertErrorShape(await post({ name: '', totalSeats: 10 }), 400, 'CLASS_NAME_REQUIRED');
    assertErrorShape(await post({ name: '   ', totalSeats: 10 }), 400, 'CLASS_NAME_REQUIRED');
    assertErrorShape(await post({ name: 'x'.repeat(101), totalSeats: 10 }), 400, 'CLASS_NAME_TOO_LONG');
    assertErrorShape(await post({ name: 'Bad seats', totalSeats: 0 }), 400, 'INVALID_TOTAL_SEATS');
    assertErrorShape(await post({ name: 'Bad seats', totalSeats: -5 }), 400, 'INVALID_TOTAL_SEATS');
    assertErrorShape(await post({ name: 'Bad seats', totalSeats: 100001 }), 400, 'INVALID_TOTAL_SEATS');
    assertErrorShape(await post({ name: 'Bad shift', shift: 'Night-Shift', totalSeats: 10 }), 400, 'INVALID_SHIFT');
    assertErrorShape(await post({ name: 'Bad seats', totalSeats: 'many' }), 400);
    assertErrorShape(await call('POST', '/classes', { raw: '{ not json', token }), 400);
  });
});

describe('Admin login', () => {
  it('rejects a wrong password with 401 INVALID_CREDENTIALS', async () => {
    const response = await call('POST', '/admin/login', { body: { username: 'admin', password: 'definitely-wrong' } });
    assertErrorShape(response, 401, 'INVALID_CREDENTIALS');
  });

  it('gives a token for the right credentials', async () => {
    assert.ok((await adminToken()).length > 10);
  });

  it(ADMIN_AUTH_REQUIRED ? 'POST /classes without a valid token is 401' : 'POST /classes is open when ADMIN_AUTH_REQUIRED=false', async () => {
    const body = { name: 'Anonymous attempt', totalSeats: 5 };
    const anonymous = await call('POST', '/classes', { body });
    const garbage = await call('POST', '/classes', { body, token: 'not-a-real-token' });

    if (ADMIN_AUTH_REQUIRED) {
      assertErrorShape(anonymous, 401, 'ADMIN_AUTH_REQUIRED');
      assertErrorShape(garbage, 401, 'ADMIN_AUTH_REQUIRED');
    } else {
      assert.equal(anonymous.status, 201);
    }
  });

  it('reading classes and applying never needs an admin token', async () => {
    const created = await createClass({ seats: 2 });
    assert.equal((await call('GET', '/classes')).status, 200);
    assert.equal((await call('GET', `/classes/${created.id}`)).status, 200);
    assert.equal((await apply(created.id, newStudentId())).status, 201);
  });
});

describe('GET /classes and GET /classes/{id}', () => {
  it('lists classes with the seat counts', async () => {
    const created = await createClass({ seats: 7, name: 'Listed class' });
    const list = await call('GET', '/classes');
    assert.equal(list.status, 200);
    assert.ok(Array.isArray(list.body));

    const row = list.body.find((c) => c.id === created.id);
    assert.ok(row, 'the new class must be in the list');
    assert.deepEqual(
      { name: row.name, totalSeats: row.totalSeats, heldSeats: row.heldSeats, confirmedSeats: row.confirmedSeats, availableSeats: row.availableSeats },
      { name: 'Listed class', totalSeats: 7, heldSeats: 0, confirmedSeats: 0, availableSeats: 7 },
    );
  });

  it('returns one class, and 404 for an unknown one', async () => {
    const created = await createClass({ seats: 3 });
    const one = await call('GET', `/classes/${created.id}`);
    assert.equal(one.status, 200);
    assert.equal(one.body.id, created.id);
    assertSeatMath(one.body);

    assertErrorShape(await call('GET', '/classes/c_doesnotexist'), 404, 'CLASS_NOT_FOUND');
  });
});

describe('POST /classes/{id}/holds (apply)', () => {
  it('returns 201 with holdId, classId, studentId, status HELD and expiresAt', async () => {
    const created = await createClass({ seats: 5 });
    const studentId = newStudentId();
    const response = await apply(created.id, studentId);

    assert.equal(response.status, 201);
    assert.match(response.body.holdId, /\S+/);
    assert.equal(response.body.classId, created.id);
    assert.equal(response.body.studentId, studentId);
    assert.equal(response.body.status, 'HELD');

    const secondsLeft = (new Date(response.body.expiresAt).getTime() - Date.now()) / 1000;
    assert.ok(!Number.isNaN(secondsLeft), 'expiresAt must be an ISO date');
    assert.ok(Math.abs(secondsLeft - HOLD_SECONDS) < 30, `expiresAt should be about ${HOLD_SECONDS}s ahead, got ${secondsLeft.toFixed(1)}s`);
  });

  it('holding a seat updates the counts', async () => {
    const created = await createClass({ seats: 4 });
    await applyOk(created.id, newStudentId());
    const summary = await getClass(created.id);

    assert.equal(summary.heldSeats, 1);
    assert.equal(summary.confirmedSeats, 0);
    assert.equal(summary.availableSeats, 3);
  });

  it('needs a valid 17-digit X-Student-Id (400)', async () => {
    const created = await createClass({ seats: 5 });
    assertErrorShape(await call('POST', `/classes/${created.id}/holds`), 400, 'STUDENT_ID_REQUIRED');
    assertErrorShape(await apply(created.id, '12345'), 400, 'STUDENT_ID_INVALID');
    assertErrorShape(await apply(created.id, '2015269123456789a'), 400, 'STUDENT_ID_INVALID');
    assertErrorShape(await apply(created.id, '201526912345678901'), 400, 'STUDENT_ID_INVALID');
    assert.equal((await getClass(created.id)).heldSeats, 0, 'rejected requests must not hold seats');
  });

  it('returns 404 for an unknown class', async () => {
    assertErrorShape(await apply('c_doesnotexist', newStudentId()), 404, 'CLASS_NOT_FOUND');
  });

  it('returns 409 NO_SEATS when the class is full', async () => {
    const created = await createClass({ seats: 1 });
    await applyOk(created.id, newStudentId());
    assertErrorShape(await apply(created.id, newStudentId()), 409, 'NO_SEATS');
    assert.equal((await getClass(created.id)).heldSeats, 1);
  });

  it('does not let one student hold the same class twice (409 ALREADY_APPLIED)', async () => {
    const created = await createClass({ seats: 5 });
    const studentId = newStudentId();
    await applyOk(created.id, studentId);
    assertErrorShape(await apply(created.id, studentId), 409, 'ALREADY_APPLIED');
    assert.equal((await getClass(created.id)).heldSeats, 1);
  });

  it('lets one student hold seats in different classes while nothing is confirmed', async () => {
    const a = await createClass({ seats: 5 });
    const b = await createClass({ seats: 5 });
    const studentId = newStudentId();
    await applyOk(a.id, studentId);
    await applyOk(b.id, studentId);
  });
});

describe('GET /holds/{id}', () => {
  it('returns holdId, classId, status and expiresAt', async () => {
    const created = await createClass({ seats: 5 });
    const hold = await applyOk(created.id, newStudentId());
    const response = await getHold(hold.holdId);

    assert.equal(response.status, 200);
    assert.equal(response.body.holdId, hold.holdId);
    assert.equal(response.body.classId, created.id);
    assert.equal(response.body.status, 'HELD');
    assert.equal(response.body.expiresAt, hold.expiresAt);
  });

  it('returns 404 for an unknown application', async () => {
    assertErrorShape(await getHold('h_doesnotexist'), 404, 'HOLD_NOT_FOUND');
  });
});

describe('POST /holds/{id}/confirm (pay the fee)', () => {
  it('confirms the admission and moves the seat from held to confirmed', async () => {
    const created = await createClass({ seats: 3 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);

    const response = await confirm(hold.holdId, studentId);
    assert.equal(response.status, 200);
    assert.deepEqual({ holdId: response.body.holdId, status: response.body.status }, { holdId: hold.holdId, status: 'CONFIRMED' });

    const summary = await getClass(created.id);
    assert.equal(summary.heldSeats, 0);
    assert.equal(summary.confirmedSeats, 1);
    assert.equal(summary.availableSeats, 2);
    assert.equal((await getHold(hold.holdId)).body.status, 'CONFIRMED');
  });

  it('is idempotent: confirming again keeps the seat counted once', async () => {
    const created = await createClass({ seats: 3 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);

    assert.equal((await confirm(hold.holdId, studentId)).status, 200);
    const again = await confirm(hold.holdId, studentId);
    assert.equal(again.status, 200);
    assert.equal(again.body.status, 'CONFIRMED');
    assert.equal((await getClass(created.id)).confirmedSeats, 1);
  });

  it('only the owner can confirm (403), and the header is required (400)', async () => {
    const created = await createClass({ seats: 3 });
    const hold = await applyOk(created.id, newStudentId());

    assertErrorShape(await confirm(hold.holdId, newStudentId()), 403, 'NOT_OWNER');
    assertErrorShape(await call('POST', `/holds/${hold.holdId}/confirm`), 400, 'STUDENT_ID_REQUIRED');
    assert.equal((await getHold(hold.holdId)).body.status, 'HELD');
  });

  it('returns 404 for an unknown application', async () => {
    assertErrorShape(await confirm('h_doesnotexist', newStudentId()), 404, 'HOLD_NOT_FOUND');
  });
});

describe('DELETE /holds/{id} (cancel)', () => {
  it('returns 204, frees the seat and marks the application CANCELLED', async () => {
    const created = await createClass({ seats: 1 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);

    const response = await cancel(hold.holdId, studentId);
    assert.equal(response.status, 204);
    assert.equal(response.text, '');

    assert.equal((await getHold(hold.holdId)).body.status, 'CANCELLED');
    assert.equal((await getClass(created.id)).availableSeats, 1);
    // The freed seat can be taken straight away, even by the same student.
    assert.equal((await apply(created.id, studentId)).status, 201);
  });

  it('cancelling twice is fine (204)', async () => {
    const created = await createClass({ seats: 2 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);
    assert.equal((await cancel(hold.holdId, studentId)).status, 204);
    assert.equal((await cancel(hold.holdId, studentId)).status, 204);
  });

  it('only the owner can cancel (403); the header is required (400); unknown is 404', async () => {
    const created = await createClass({ seats: 2 });
    const hold = await applyOk(created.id, newStudentId());

    assertErrorShape(await cancel(hold.holdId, newStudentId()), 403, 'NOT_OWNER');
    assertErrorShape(await call('DELETE', `/holds/${hold.holdId}`), 400, 'STUDENT_ID_REQUIRED');
    assertErrorShape(await cancel('h_doesnotexist', newStudentId()), 404, 'HOLD_NOT_FOUND');
  });

  it('a confirmed admission cannot be cancelled, and a cancelled one cannot be confirmed (409)', async () => {
    const created = await createClass({ seats: 3 });

    const confirmedStudent = newStudentId();
    const confirmedHold = await applyOk(created.id, confirmedStudent);
    await confirm(confirmedHold.holdId, confirmedStudent);
    assertErrorShape(await cancel(confirmedHold.holdId, confirmedStudent), 409, 'INVALID_STATE');

    const cancelledStudent = newStudentId();
    const cancelledHold = await applyOk(created.id, cancelledStudent);
    await cancel(cancelledHold.holdId, cancelledStudent);
    assertErrorShape(await confirm(cancelledHold.holdId, cancelledStudent), 409, 'INVALID_STATE');
  });
});

describe('GET /me/holds', () => {
  it('lists only this student\'s applications with the class name and status', async () => {
    const a = await createClass({ seats: 3, name: 'Mine A', shift: 'Morning-Shift' });
    const b = await createClass({ seats: 3, name: 'Mine B', shift: 'Day-Shift' });
    const me = newStudentId();
    const holdA = await applyOk(a.id, me);
    await applyOk(b.id, me);
    await applyOk(a.id, newStudentId()); // someone else's application

    const response = await myHolds(me);
    assert.equal(response.status, 200);
    assert.equal(response.body.length, 2);

    const row = response.body.find((h) => h.holdId === holdA.holdId);
    assert.equal(row.classId, a.id);
    assert.equal(row.className, 'Mine A');
    assert.equal(row.status, 'HELD');
    assert.ok(row.expiresAt);
  });

  it('is an empty list for a new student, and needs the header (400)', async () => {
    const response = await myHolds(newStudentId());
    assert.equal(response.status, 200);
    assert.deepEqual(response.body, []);
    assertErrorShape(await call('GET', '/me/holds'), 400, 'STUDENT_ID_REQUIRED');
  });

  it('keeps cancelled applications in the list with their status', async () => {
    const created = await createClass({ seats: 2 });
    const me = newStudentId();
    const hold = await applyOk(created.id, me);
    await cancel(hold.holdId, me);

    const row = (await myHolds(me)).body.find((h) => h.holdId === hold.holdId);
    assert.equal(row.status, 'CANCELLED');
  });
});

describe('One confirmed class per student', () => {
  it('a confirmed student cannot apply to another class (409 ALREADY_CONFIRMED)', async () => {
    const a = await createClass({ seats: 3 });
    const b = await createClass({ seats: 3 });
    const studentId = newStudentId();
    const hold = await applyOk(a.id, studentId);
    await confirm(hold.holdId, studentId);

    const blocked = await apply(b.id, studentId);
    assertErrorShape(blocked, 409, 'ALREADY_CONFIRMED');
    assert.match(blocked.body.message, /Confirmed/);
    assert.equal((await getClass(b.id)).heldSeats, 0);
    assertErrorShape(await apply(a.id, studentId), 409, 'ALREADY_CONFIRMED');
  });

  it('a student holding two classes can confirm only one', async () => {
    const a = await createClass({ seats: 3 });
    const b = await createClass({ seats: 3 });
    const studentId = newStudentId();
    const holdA = await applyOk(a.id, studentId);
    const holdB = await applyOk(b.id, studentId);

    assert.equal((await confirm(holdA.holdId, studentId)).status, 200);
    assertErrorShape(await confirm(holdB.holdId, studentId), 409, 'ALREADY_CONFIRMED');
    assert.equal((await getHold(holdB.holdId)).body.status, 'HELD');
  });

  it('does not affect other students', async () => {
    const a = await createClass({ seats: 3 });
    const b = await createClass({ seats: 3 });
    const confirmedStudent = newStudentId();
    await confirm((await applyOk(a.id, confirmedStudent)).holdId, confirmedStudent);

    const other = newStudentId();
    assert.equal((await apply(a.id, other)).status, 201);
    assert.equal((await apply(b.id, other)).status, 201);
  });
});

describe('Expiry (no API call needed)', { skip: HOLD_SECONDS > 15 && `needs a short hold time: start the backend with HOLD_DURATION_SECONDS=3 (now ${HOLD_SECONDS})` }, () => {
  it('an unpaid application expires by itself and its seat is released', async () => {
    const created = await createClass({ seats: 1 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);
    assert.equal((await getClass(created.id)).availableSeats, 0);

    await sleep((HOLD_SECONDS + 1.5) * 1000);

    assert.equal((await getHold(hold.holdId)).body.status, 'EXPIRED');
    const summary = await getClass(created.id);
    assert.equal(summary.heldSeats, 0);
    assert.equal(summary.availableSeats, 1);
    assert.equal((await myHolds(studentId)).body.find((h) => h.holdId === hold.holdId).status, 'EXPIRED');
  });

  it('an expired application cannot be confirmed (409) and the seat can be taken by someone else', async () => {
    const created = await createClass({ seats: 1 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);

    await sleep((HOLD_SECONDS + 1.5) * 1000);

    assertErrorShape(await confirm(hold.holdId, studentId), 409, 'HOLD_EXPIRED');
    assert.equal((await apply(created.id, newStudentId())).status, 201);
  });

  it('a student whose hold expired can apply to the same class again', async () => {
    const created = await createClass({ seats: 1 });
    const studentId = newStudentId();
    await applyOk(created.id, studentId);

    await sleep((HOLD_SECONDS + 1.5) * 1000);

    assert.equal((await apply(created.id, studentId)).status, 201);
  });
});
