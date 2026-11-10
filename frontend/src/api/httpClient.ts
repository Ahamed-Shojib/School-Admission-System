import { API_URL } from '../config/env';

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;

  constructor(status: number, code: string, message: string) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'DELETE';
  body?: unknown;
  studentId?: string;
  /** Admin session token, sent as a Bearer token. */
  adminToken?: string;
}

export async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';
  if (options.studentId) headers['X-Student-Id'] = options.studentId;
  if (options.adminToken) headers.Authorization = `Bearer ${options.adminToken}`;

  let response: Response;
  try {
    response = await fetch(`${API_URL}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    throw new ApiError(0, 'NETWORK_ERROR', 'Cannot reach the server.');
  }

  if (!response.ok) {
    let code = 'UNKNOWN_ERROR';
    let message = response.statusText;
    try {
      const body = await response.json();
      if (typeof body?.code === 'string') code = body.code;
      if (typeof body?.message === 'string') message = body.message;
    } catch {
      // Non-JSON error body: keep the defaults.
    }
    throw new ApiError(response.status, code, message);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
