// Concurrency tests: many requests at the same moment must never break the seat rules.
// Run against a running stack:  docker compose run --rm api-tests     (or: cd tests/api && node --test concurrency.test.mjs)
// Load size: CONCURRENCY_REQUESTS (default 200).
import assert from 'node:assert/strict';
import { before, describe, it } from 'node:test';
import {
  apply,
  applyOk,
  assertErrorShape,
  assertNoServerErrors,
  assertSeatMath,
  cancel,
  confirm,
  createClass,
  getClass,
  getHold,
  newStudentId,
  sleep,
  tally,
  waitForApi,
} from './helpers.mjs';

const N = Math.max(20, Number(process.env.CONCURRENCY_REQUESTS ?? 200));

before(waitForApi);

/** Fire all requests at once (they are all started before any is awaited). */
const allAtOnce = (count, makeRequest) => Promise.all(Array.from({ length: count }, (_, i) => makeRequest(i)));

describe('Seat limit under load', () => {
  it(`${N} students apply at once for 10 seats: exactly 10 win, the rest get 409 NO_SEATS`, async () => {
    const seats = 10;
    const created = await createClass({ seats });

    // While the requests run, keep reading the class: held + confirmed must never go above totalSeats.
    let running = true;
    let highestUsed = 0;
    const watcher = (async () => {
      while (running) {
        const summary = await getClass(created.id);
        highestUsed = Math.max(highestUsed, summary.heldSeats + summary.confirmedSeats);
        await sleep(15);
      }
    })();

    const responses = await allAtOnce(N, () => apply(created.id, newStudentId()));
    running = false;
    await watcher;

    assertNoServerErrors(responses);
    assert.deepEqual(tally(responses), { 201: seats, 409: N - seats }, `unexpected result mix: ${JSON.stringify(tally(responses))}`);
    for (const lost of responses.filter((r) => r.status === 409)) assert.equal(lost.body.code, 'NO_SEATS');

    assert.ok(highestUsed <= seats, `the class was oversold: ${highestUsed} of ${seats} seats in use at once`);
    const summary = await getClass(created.id);
    assertSeatMath(summary);
    assert.equal(summary.heldSeats, seats);
    assert.equal(summary.availableSeats, 0);
  });

  it('a class with a single seat goes to exactly one of many applicants', async () => {
    const created = await createClass({ seats: 1 });

    const responses = await allAtOnce(N, () => apply(created.id, newStudentId()));

    assertNoServerErrors(responses);
    assert.deepEqual(tally(responses), { 201: 1, 409: N - 1 });
    assert.equal((await getClass(created.id)).heldSeats, 1);
  });

  it('several classes filling up together each keep to their own limit', async () => {
    const classes = await Promise.all(Array.from({ length: 5 }, () => createClass({ seats: 4 })));
    const perClass = Math.ceil(N / 5);

    // Requests for all five classes are interleaved.
    const responses = await allAtOnce(perClass * 5, (i) => {
      const target = classes[i % 5];
      return apply(target.id, newStudentId()).then((r) => ({ ...r, classId: target.id }));
    });

    assertNoServerErrors(responses);
    for (const c of classes) {
      const mine = responses.filter((r) => r.classId === c.id);
      assert.equal(mine.filter((r) => r.status === 201).length, 4, `class ${c.id} should have sold exactly 4 seats`);
      const summary = await getClass(c.id);
      assertSeatMath(summary);
      assert.equal(summary.heldSeats, 4);
    }
  });

  it('one student sending many applications at once gets a single seat', async () => {
    const created = await createClass({ seats: 20 });
    const studentId = newStudentId();

    const responses = await allAtOnce(30, () => apply(created.id, studentId));

    assertNoServerErrors(responses);
    assert.deepEqual(tally(responses), { 201: 1, 409: 29 });
    for (const dup of responses.filter((r) => r.status === 409)) assert.equal(dup.body.code, 'ALREADY_APPLIED');
    assert.equal((await getClass(created.id)).heldSeats, 1);
  });
});

describe('Confirm, cancel and release under load', () => {
  it('confirming the same application many times at once counts one seat', async () => {
    const created = await createClass({ seats: 3 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);

    const responses = await allAtOnce(30, () => confirm(hold.holdId, studentId));

    assertNoServerErrors(responses);
    assert.deepEqual(tally(responses), { 200: 30 });
    const summary = await getClass(created.id);
    assert.equal(summary.confirmedSeats, 1);
    assert.equal(summary.heldSeats, 0);
    assert.equal(summary.availableSeats, 2);
  });

  it('confirm and cancel racing on one application leave a consistent result', async () => {
    const created = await createClass({ seats: 2 });
    const studentId = newStudentId();
    const hold = await applyOk(created.id, studentId);

    const responses = await allAtOnce(20, (i) => (i % 2 === 0 ? confirm(hold.holdId, studentId) : cancel(hold.holdId, studentId)));

    assertNoServerErrors(responses);
    for (const r of responses) assert.ok([200, 204, 409].includes(r.status), `unexpected status ${r.status}: ${r.text}`);

    const finalStatus = (await getHold(hold.holdId)).body.status;
    assert.ok(['CONFIRMED', 'CANCELLED'].includes(finalStatus), `ended in ${finalStatus}`);
    const summary = await getClass(created.id);
    assertSeatMath(summary);
    assert.equal(summary.heldSeats, 0, 'nothing may stay held');
    assert.equal(summary.confirmedSeats, finalStatus === 'CONFIRMED' ? 1 : 0);
  });

  it('a seat released while many are waiting is taken by at most one of them', async () => {
    const created = await createClass({ seats: 1 });
    const holder = newStudentId();
    const hold = await applyOk(created.id, holder);

    // 40 applications race with the cancel that frees the only seat.
    const [cancelResult, ...wave] = await Promise.all([
      cancel(hold.holdId, holder),
      ...Array.from({ length: 40 }, () => apply(created.id, newStudentId())),
    ]);

    assert.equal(cancelResult.status, 204);
    assertNoServerErrors(wave);
    const winners = wave.filter((r) => r.status === 201).length;
    assert.ok(winners <= 1, `${winners} students got the one seat`);

    if (winners === 0) assert.equal((await apply(created.id, newStudentId())).status, 201, 'the freed seat must be available');
    const summary = await getClass(created.id);
    assertSeatMath(summary);
    assert.equal(summary.heldSeats, 1);
  });

  it('a mixed storm of applies, confirms and cancels never oversells', async () => {
    const seats = 8;
    const created = await createClass({ seats });
    const students = Array.from({ length: seats }, () => newStudentId());
    const holds = await Promise.all(students.map((s) => applyOk(created.id, s)));
    assert.equal((await getClass(created.id)).availableSeats, 0);

    // Half confirm, half cancel, while 100 newcomers keep trying to get in.
    const storm = await Promise.all([
      ...holds.map((h, i) => (i % 2 === 0 ? confirm(h.holdId, students[i]) : cancel(h.holdId, students[i]))),
      ...Array.from({ length: 100 }, () => apply(created.id, newStudentId())),
    ]);

    assertNoServerErrors(storm);
    const summary = await getClass(created.id);
    assertSeatMath(summary);
    assert.ok(summary.heldSeats + summary.confirmedSeats <= seats, `oversold: ${JSON.stringify(summary)}`);
    assert.equal(summary.confirmedSeats, seats / 2, 'every confirm that was sent must have succeeded');
  });
});

describe('One confirmed class per student under load', () => {
  it('a student confirming two held classes at the same moment is confirmed in exactly one', async () => {
    for (let round = 0; round < 5; round++) {
      const a = await createClass({ seats: 5 });
      const b = await createClass({ seats: 5 });
      const studentId = newStudentId();
      const holdA = await applyOk(a.id, studentId);
      const holdB = await applyOk(b.id, studentId);

      const results = await Promise.all([confirm(holdA.holdId, studentId), confirm(holdB.holdId, studentId)]);

      assertNoServerErrors(results);
      assert.deepEqual(tally(results), { 200: 1, 409: 1 }, `round ${round}: ${JSON.stringify(tally(results))}`);
      assertErrorShape(results.find((r) => r.status === 409), 409, 'ALREADY_CONFIRMED');

      const confirmedTotal = (await getClass(a.id)).confirmedSeats + (await getClass(b.id)).confirmedSeats;
      assert.equal(confirmedTotal, 1, `round ${round}: the student is confirmed in ${confirmedTotal} classes`);
    }
  });

  it('a student applying to many classes while being confirmed ends up confirmed in one class only', async () => {
    const classes = await Promise.all(Array.from({ length: 6 }, () => createClass({ seats: 5 })));
    const studentId = newStudentId();
    const firstHold = await applyOk(classes[0].id, studentId);

    // Confirm class 0 while the same student tries to apply to the other five.
    const results = await Promise.all([
      confirm(firstHold.holdId, studentId),
      ...classes.slice(1).map((c) => apply(c.id, studentId)),
    ]);

    assertNoServerErrors(results);
    assert.equal(results[0].status, 200);

    // Whatever got through before the confirm may stay held, but none of it can ever be confirmed.
    for (const r of results.slice(1)) {
      if (r.status === 201) assertErrorShape(await confirm(r.body.holdId, studentId), 409, 'ALREADY_CONFIRMED');
      else assertErrorShape(r, 409, 'ALREADY_CONFIRMED');
    }
    let confirmedTotal = 0;
    for (const c of classes) confirmedTotal += (await getClass(c.id)).confirmedSeats;
    assert.equal(confirmedTotal, 1);
  });
});
