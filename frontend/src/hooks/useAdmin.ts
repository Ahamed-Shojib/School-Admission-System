import { useCallback, useEffect, useState } from 'react';
import type { AdminSession } from '../api/adminApi';

const STORAGE_KEY = 'adminSession';

function readStored(): AdminSession | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw) as AdminSession;
    return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
  } catch {
    return null;
  }
}

/**
 * Admin session. Kept in sessionStorage: it survives a page refresh but ends when the tab is closed.
 * This only controls what the UI shows; the backend checks the token itself on every admin action.
 */
export function useAdmin() {
  const [session, setSession] = useState<AdminSession | null>(readStored);

  const login = useCallback((next: AdminSession) => {
    try {
      sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next));
    } catch {
      // Storage unavailable: the session still works until the page is closed.
    }
    setSession(next);
  }, []);

  const logout = useCallback(() => {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch {
      // Nothing to clean up.
    }
    setSession(null);
  }, []);

  // Sign out by itself when the token runs out.
  useEffect(() => {
    if (!session) return;
    const msLeft = new Date(session.expiresAt).getTime() - Date.now();
    if (msLeft <= 0) {
      logout();
      return;
    }
    const timer = window.setTimeout(logout, Math.min(msLeft, 2_000_000_000));
    return () => window.clearTimeout(timer);
  }, [session, logout]);

  return { isAdmin: session !== null, token: session?.token ?? '', login, logout };
}
