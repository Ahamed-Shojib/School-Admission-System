import assert from 'node:assert/strict';
import { randomInt } from 'node:crypto';

// ---- settings (all optional; they match the docker-compose defaults) ----
export const API_URL = (process.env.API_URL ?? 'http://localhost:8080').replace(/\/+$/, '');
export const HOLD_SECONDS = Number(process.env.HOLD_DURATION_SECONDS ?? 600);
export const ADMIN_AUTH_REQUIRED = (process.env.ADMIN_AUTH_REQUIRED ?? 'true').toLowerCase() !== 'false';
const ADMIN_USERNAME = process.env.ADMIN_USERNAME ?? 'admin';
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? 'admin123';

export const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

/** One HTTP call. Never throws on an HTTP error status; returns { status, body, text }. */
export async function call(method, path, { body, raw, studentId, token } = {}) {
  const headers = { Accept: 'application/json' };
  if (body !== undefined || raw !== undefined) headers['Content-Type'] = 'application/json';
  if (studentId) headers['X-Student-Id'] = studentId;
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${API_URL}${path}`, {
    method,
    headers,
    body: raw !== undefined ? raw : body !== undefined ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    // not JSON
  }
  return { status: response.status, body: json, text };
}

/** Waits until the API answers, so the tests can be started together with the stack. */
export async function waitForApi(timeoutMs = 120_000) {
  const started = Date.now();
  for (;;) {
    try {
      const response = await fetch(`${API_URL}/health`);
      if (response.ok) return;
    } catch {
      // not up yet
    }
    if (Date.now() - started > timeoutMs) {
      throw new Error(`The API at ${API_URL} did not become ready within ${timeoutMs / 1000}s.`);
    }
    await sleep(1000);
  }
}

let cachedToken;
/** Logs in as admin once. Needed to add classes when ADMIN_AUTH_REQUIRED is true. */
export async function adminToken() {
  if (cachedToken) return cachedToken;
  const login = await call('POST', '/admin/login', { body: { username: ADMIN_USERNAME, password: ADMIN_PASSWORD } });
  assert.equal(login.status, 200, `Admin login failed (${login.status}). Set ADMIN_USERNAME / ADMIN_PASSWORD to match the backend.`);
  cachedToken = login.body.token;
  return cachedToken;
}

/** A random 17-digit birth registration number. */
export function newStudentId() {
  return String(randomInt(1, 10)) + Array.from({ length: 16 }, () => randomInt(0, 10)).join('');
}

export async function createClass({ seats = 10, name, shift = 'Morning-Shift' } = {}) {
  const response = await call('POST', '/classes', {
    body: { name: name ?? `Test class ${Date.now()}-${randomInt(1_000_000)}`, shift, totalSeats: seats },
    token: await adminToken(),
  });
  assert.equal(response.status, 201, `Could not create a class: ${response.status} ${response.text}`);
  return response.body;
}

export const apply = (classId, studentId) => call('POST', `/classes/${classId}/holds`, { studentId });
export const confirm = (holdId, studentId) => call('POST', `/holds/${holdId}/confirm`, { studentId });
export const cancel = (holdId, studentId) => call('DELETE', `/holds/${holdId}`, { studentId });
export const getHold = (holdId) => call('GET', `/holds/${holdId}`);
export const myHolds = (studentId) => call('GET', '/me/holds', { studentId });

export async function getClass(classId) {
  const response = await call('GET', `/classes/${classId}`);
  assert.equal(response.status, 200, `GET /classes/${classId} returned ${response.status}`);
  return response.body;
}

/** Apply and require success; returns the hold. */
export async function applyOk(classId, studentId) {
  const response = await apply(classId, studentId);
  assert.equal(response.status, 201, `Apply failed: ${response.status} ${response.text}`);
  return response.body;
}

/** How many responses had each status code, e.g. { 201: 10, 409: 190 }. */
export function tally(responses) {
  const counts = {};
  for (const r of responses) counts[r.status] = (counts[r.status] ?? 0) + 1;
  return counts;
}

export function assertNoServerErrors(responses) {
  const bad = responses.filter((r) => r.status >= 500);
  assert.equal(bad.length, 0, `${bad.length} request(s) failed with a 5xx, e.g. ${bad[0]?.status} ${bad[0]?.text}`);
}

/** Every error response must be { code, message }. */
export function assertErrorShape(response, status, code) {
  assert.equal(response.status, status, `expected ${status}, got ${response.status}: ${response.text}`);
  assert.equal(typeof response.body?.code, 'string', 'error body needs a string "code"');
  assert.equal(typeof response.body?.message, 'string', 'error body needs a string "message"');
  if (code) assert.equal(response.body.code, code);
}

/** held + confirmed + available must equal total, and none may be negative. */
export function assertSeatMath(summary) {
  const { totalSeats, heldSeats, confirmedSeats, availableSeats } = summary;
  assert.ok(heldSeats >= 0 && confirmedSeats >= 0 && availableSeats >= 0, `negative seat count: ${JSON.stringify(summary)}`);
  assert.equal(heldSeats + confirmedSeats + availableSeats, totalSeats, `seat math broken: ${JSON.stringify(summary)}`);
}
