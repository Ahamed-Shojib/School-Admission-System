import { useCallback, useEffect, useRef, useState } from 'react';

/**
 * Loads data now and then every `intervalMs` (0 = load once, no repeat).
 * `resetKey` clears the old data when what is being watched changes. `enabled = false` pauses everything.
 * A failed poll keeps the last good data and exposes the error.
 */
export function usePolling<T>(
  fetcher: () => Promise<T>,
  intervalMs: number,
  resetKey: string,
  enabled = true,
) {
  const [data, setData] = useState<T | undefined>(undefined);
  const [error, setError] = useState<unknown>(undefined);
  const [loading, setLoading] = useState(enabled);

  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;
  const aliveRef = useRef(true);
  const keyRef = useRef(resetKey);

  const refresh = useCallback(async () => {
    try {
      const result = await fetcherRef.current();
      if (aliveRef.current) {
        setData(result);
        setError(undefined);
      }
    } catch (e) {
      if (aliveRef.current) setError(e);
    } finally {
      if (aliveRef.current) setLoading(false);
    }
  }, []);

  useEffect(() => {
    aliveRef.current = true;

    if (keyRef.current !== resetKey) {
      keyRef.current = resetKey;
      setData(undefined);
      setError(undefined);
    }

    if (!enabled) {
      setLoading(false);
      return () => {
        aliveRef.current = false;
      };
    }

    void refresh();
    const timer = intervalMs > 0 ? window.setInterval(() => void refresh(), intervalMs) : undefined;

    return () => {
      aliveRef.current = false;
      if (timer !== undefined) window.clearInterval(timer);
    };
  }, [intervalMs, resetKey, enabled, refresh]);

  return { data, error, loading, refresh };
}
