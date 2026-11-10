import { useCallback, useState } from 'react';
import { STUDENT_ID_LENGTH } from '../config/env';

const STORAGE_KEY = 'studentId';

export const isValidStudentId = (value: string) => new RegExp(`^\\d{${STUDENT_ID_LENGTH}}$`).test(value);

function readStored(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? '';
  } catch {
    return '';
  }
}

/** The student's birth registration number, kept in localStorage so a refresh or a closed browser keeps it. */
export function useStudentId() {
  const [studentId, setStudentIdState] = useState<string>(readStored);

  const setStudentId = useCallback((value: string) => {
    const digits = value.replace(/\D/g, '').slice(0, STUDENT_ID_LENGTH);
    setStudentIdState(digits);
    try {
      localStorage.setItem(STORAGE_KEY, digits);
    } catch {
      // Storage unavailable: the value still works for this session.
    }
  }, []);

  return { studentId, setStudentId, isValid: isValidStudentId(studentId) };
}
