import { request } from './httpClient';

export interface AdminSession {
  token: string;
  expiresAt: string;
}

export const adminLogin = (username: string, password: string) =>
  request<AdminSession>('/admin/login', { method: 'POST', body: { username, password } });
