import { useCallback, useEffect, useState } from 'react';

const read = () => window.location.hash.replace(/^#/, '') || '/classes';

/** Minimal hash router (#/classes, #/me ...). Hash URLs survive a refresh with no server setup. */
export function useHashRoute() {
  const [path, setPath] = useState(read);

  useEffect(() => {
    const onChange = () => setPath(read());
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);

  const navigate = useCallback((to: string) => {
    window.location.hash = to;
  }, []);

  return { path, navigate };
}
