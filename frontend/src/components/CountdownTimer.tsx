import { useCountdown } from '../hooks/useCountdown';
import { formatRemaining } from '../utils/formatTime';

export function CountdownTimer({ expiresAt }: { expiresAt: string }) {
  const remaining = useCountdown(expiresAt);
  if (remaining === null) return null;
  return (
    <span className="countdown" aria-label="Time left">
      {formatRemaining(remaining)}
    </span>
  );
}
