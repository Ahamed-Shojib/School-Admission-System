import { useEffect, useState } from 'react';

/** Milliseconds left until `expiresAt`, ticking every half second. Null while there is no deadline. */
export function useCountdown(expiresAt: string | undefined): number | null {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    if (!expiresAt) return;
    setNow(Date.now());
    const timer = window.setInterval(() => setNow(Date.now()), 500);
    return () => window.clearInterval(timer);
  }, [expiresAt]);

  if (!expiresAt) return null;
  return Math.max(0, new Date(expiresAt).getTime() - now);
}
