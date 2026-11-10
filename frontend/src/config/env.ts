// Backend URL: runtime config.js (Docker) > VITE_API_URL (dev) > default.
export const API_URL: string = (
  window.__APP_CONFIG__?.API_URL ||
  import.meta.env.VITE_API_URL ||
  'http://localhost:8080'
).replace(/\/+$/, '');

export const STUDENT_ID_LENGTH = 17;
export const CLASS_POLL_MS = 3000;
export const HOLD_POLL_MS = 2000;
