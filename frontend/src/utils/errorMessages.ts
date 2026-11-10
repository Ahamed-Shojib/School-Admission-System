import { ApiError } from '../api/httpClient';

const MESSAGES: Record<string, string> = {
  NETWORK_ERROR: 'Cannot reach the server. Please try again in a moment.',
  STUDENT_ID_REQUIRED: "Enter the student's birth registration number in the box at the top of the page.",
  STUDENT_ID_INVALID: 'The birth registration number must be exactly 17 digits.',
  CLASS_NOT_FOUND: 'This class could not be found.',
  HOLD_NOT_FOUND: 'This application could not be found.',
  NO_SEATS: 'Sorry, there are no seats left in this class.',
  ALREADY_APPLIED: 'This student has already applied to this class.',
  ALREADY_CONFIRMED:
    'This birth registration number is already registered with status Confirmed, so it cannot be used to apply to another class.',
  HOLD_EXPIRED: 'The application has expired. Please apply again.',
  NOT_OWNER: 'This application belongs to another student, so you cannot change it.',
  INVALID_STATE: 'This action is not available for this application right now.',
  CLASS_NAME_REQUIRED: 'Enter a class name.',
  CLASS_NAME_TOO_LONG: 'The class name can be at most 100 characters.',
  INVALID_TOTAL_SEATS: 'Seats must be a whole number between 1 and 100,000.',
  INVALID_SHIFT: 'Choose Morning-Shift or Day-Shift.',
  INVALID_CREDENTIALS: 'Incorrect username or password.',
  ADMIN_AUTH_REQUIRED: 'Your admin session has ended. Please log in again.',
  VALIDATION_ERROR: 'Some of the information is not valid. Please check it and try again.',
  INTERNAL_ERROR: 'Something went wrong on the server. Please try again in a moment.',
};

const FALLBACK = 'Something went wrong. Please try again.';

/** Turns any error into a message a parent can understand. */
export function toUserMessage(error: unknown): string {
  if (error instanceof ApiError) {
    // This one names the class the student is confirmed in, so the server's own (English) text is shown.
    if (error.code === 'ALREADY_CONFIRMED' && error.message) return error.message;
    return MESSAGES[error.code] ?? FALLBACK;
  }
  return FALLBACK;
}
