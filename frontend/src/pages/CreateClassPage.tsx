import { useState } from 'react';
import { createClass } from '../api/classesApi';
import { ApiError } from '../api/httpClient';
import { ClassForm } from '../components/ClassForm';
import { toUserMessage } from '../utils/errorMessages';

interface Props {
  adminToken: string;
  onSessionEnded: () => void;
  navigate: (to: string) => void;
}

/** Admin section. Only reachable after admin login; the backend also checks the token. */
export function CreateClassPage({ adminToken, onSessionEnded, navigate }: Props) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [formKey, setFormKey] = useState(0); // changing the key empties the form after a successful add

  const submit = async (name: string, shift: string, totalSeats: number) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await createClass(name, shift, totalSeats, adminToken);
      setNotice(`Class "${name}" (${shift}) with ${totalSeats} seats was added.`);
      setFormKey((k) => k + 1);
    } catch (e) {
      if (e instanceof ApiError && e.code === 'ADMIN_AUTH_REQUIRED') {
        onSessionEnded();
        navigate('/admin/login');
        return;
      }
      setError(toUserMessage(e));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section>
      <h2>Admin: add a class</h2>
      <p className="hint">Each shift is its own class with its own seats, for example "Class 6" in Morning-Shift and "Class 6" in Day-Shift.</p>
      {notice && <p className="message message-ok">{notice}</p>}
      <ClassForm key={formKey} busy={busy} error={error} onSubmit={submit} />
      <p>
        <button type="button" className="link" onClick={() => navigate('/classes')}>
          View the class list
        </button>
      </p>
    </section>
  );
}
