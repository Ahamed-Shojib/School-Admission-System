import { ApiError } from '../src/api/httpClient';
import { toUserMessage } from '../src/utils/errorMessages';
import { effectiveStatus, formatRemaining } from '../src/utils/formatTime';

describe('formatRemaining', () => {
  it('formats mm:ss and never goes negative', () => {
    expect(formatRemaining(600_000)).toBe('10:00');
    expect(formatRemaining(61_000)).toBe('01:01');
    expect(formatRemaining(-5)).toBe('00:00');
  });
});

describe('effectiveStatus', () => {
  const expiresAt = '2026-11-01T04:10:00Z';
  const at = (iso: string) => new Date(iso).getTime();

  it('turns HELD into EXPIRED once the deadline passes, without a reload', () => {
    expect(effectiveStatus('HELD', expiresAt, at('2026-11-01T04:09:59Z'))).toBe('HELD');
    expect(effectiveStatus('HELD', expiresAt, at('2026-11-01T04:10:00Z'))).toBe('EXPIRED');
  });

  it('leaves other statuses alone', () => {
    expect(effectiveStatus('CONFIRMED', expiresAt, at('2026-11-02T00:00:00Z'))).toBe('CONFIRMED');
  });
});

describe('toUserMessage', () => {
  it('maps known codes to a readable English message and unknown ones to a safe fallback', () => {
    expect(toUserMessage(new ApiError(409, 'NO_SEATS', 'x'))).toContain('no seats');
    expect(toUserMessage(new ApiError(0, 'NETWORK_ERROR', 'x'))).toContain('server');
    expect(toUserMessage(new ApiError(401, 'ADMIN_AUTH_REQUIRED', 'x'))).toContain('log in again');
    expect(toUserMessage(new Error('boom'))).not.toContain('boom');
  });

  it('shows the server text for ALREADY_CONFIRMED, which names the confirmed class', () => {
    const text = 'This birth registration number is already registered for "Class 6 (Morning-Shift)" with status Confirmed, so it cannot be used to apply to another class.';
    expect(toUserMessage(new ApiError(409, 'ALREADY_CONFIRMED', text))).toBe(text);
    expect(toUserMessage(new ApiError(409, 'ALREADY_CONFIRMED', ''))).toContain('Confirmed');
  });
});
