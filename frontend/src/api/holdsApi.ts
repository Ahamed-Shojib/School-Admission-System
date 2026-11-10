import type { Hold, MyHold } from '../types/hold';
import { request } from './httpClient';

const id = encodeURIComponent;

export const applyToClass = (classId: string, studentId: string) =>
  request<Hold>(`/classes/${id(classId)}/holds`, { method: 'POST', studentId });

export const getHold = (holdId: string) => request<Hold>(`/holds/${id(holdId)}`);

export const confirmHold = (holdId: string, studentId: string) =>
  request<{ holdId: string; status: string }>(`/holds/${id(holdId)}/confirm`, { method: 'POST', studentId });

export const cancelHold = (holdId: string, studentId: string) =>
  request<void>(`/holds/${id(holdId)}`, { method: 'DELETE', studentId });

export const listMyHolds = (studentId: string) => request<MyHold[]>('/me/holds', { studentId });
