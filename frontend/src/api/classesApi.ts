import type { ClassSummary } from '../types/class';
import { request } from './httpClient';

export const createClass = (name: string, shift: string, totalSeats: number, adminToken: string) =>
  request<{ id: string; name: string; totalSeats: number; shift?: string | null }>('/classes', {
    method: 'POST',
    body: { name, shift, totalSeats },
    adminToken,
  });

export const listClasses = () => request<ClassSummary[]>('/classes');

export const getClass = (classId: string) =>
  request<ClassSummary>(`/classes/${encodeURIComponent(classId)}`);
