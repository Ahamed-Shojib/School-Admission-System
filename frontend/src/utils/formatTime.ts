import type { HoldStatus } from '../types/hold';

/** mm:ss for a remaining time in milliseconds. */
export function formatRemaining(ms: number): string {
  const totalSeconds = Math.max(0, Math.ceil(ms / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
}

/** A HELD application whose deadline has passed is shown as EXPIRED immediately, without waiting for the next poll. */
export function effectiveStatus(status: HoldStatus, expiresAt: string, nowMs: number): HoldStatus {
  return status === 'HELD' && new Date(expiresAt).getTime() <= nowMs ? 'EXPIRED' : status;
}
