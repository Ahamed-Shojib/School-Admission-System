import type { HoldStatus } from '../types/hold';

const LABELS: Record<HoldStatus, string> = {
  HELD: 'On hold',
  CONFIRMED: 'Admission confirmed',
  EXPIRED: 'Expired',
  CANCELLED: 'Cancelled',
};

export function HoldStatusBadge({ status }: { status: HoldStatus }) {
  return <span className={`badge badge-${status.toLowerCase()}`}>{LABELS[status]}</span>;
}
