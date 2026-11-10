import { useState } from 'react';
import { adminLogin, type AdminSession } from '../api/adminApi';
import { ErrorMessage } from '../components/ErrorMessage';
import { toUserMessage } from '../utils/errorMessages';

interface Props {
  onLoggedIn: (session: AdminSession) => void;
}

export function AdminLoginPage({ onLoggedIn }: Props) {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    if (!username.trim() || !password) {
      setError('Enter your username and password.');
      return;
    }
    setBusy(true);
    setError(null);
    try {
      onLoggedIn(await adminLogin(username.trim(), password));
    } catch (e) {
      setError(toUserMessage(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section>
      <h2>Admin login</h2>
      <p className="hint">Only school admins can add classes. Parents do not need to log in.</p>
      <form
        className="card form"
        onSubmit={(e) => {
          e.preventDefault();
          void submit();
        }}
      >
        <label htmlFor="admin-username">Username</label>
        <input id="admin-username" autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} />
        <label htmlFor="admin-password">Password</label>
        <input
          id="admin-password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />
        <ErrorMessage message={error} />
        <button type="submit" disabled={busy}>
          Log in
        </button>
      </form>
    </section>
  );
}
