import { useEffect, useState } from 'react';
import { cancelHold, confirmHold, getHold } from '../api/holdsApi';
import { CountdownTimer } from '../components/CountdownTimer';
import { ErrorMessage } from '../components/ErrorMessage';
import { HoldStatusBadge } from '../components/HoldStatusBadge';
import { Loader } from '../components/Loader';
import { HOLD_POLL_MS } from '../config/env';
import { useCountdown } from '../hooks/useCountdown';
import { usePolling } from '../hooks/usePolling';
import { toUserMessage } from '../utils/errorMessages';
import { effectiveStatus } from '../utils/formatTime';

interface Props {
  holdId: string;
  studentId: string;
  isValid: boolean;
  navigate: (to: string) => void;
}

export function HoldDetailPage({ holdId, studentId, isValid, navigate }: Props) {
  // Keep polling only while the application can still change.
  const [settled, setSettled] = useState(false);
  const { data, error, loading, refresh } = usePolling(() => getHold(holdId), settled ? 0 : HOLD_POLL_MS, holdId);

  // The countdown ticks every 500 ms, which also re-renders this page so an overdue HELD shows as EXPIRED at once.
  useCountdown(data?.expiresAt);
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const shownStatus = data ? effectiveStatus(data.status, data.expiresAt, Date.now()) : undefined;
  const terminal = shownStatus !== undefined && shownStatus !== 'HELD';

  useEffect(() => {
    if (terminal) setSettled(true);
  }, [terminal]);

  const run = async (action: () => Promise<unknown>, success: string, after?: () => void) => {
    setBusy(true);
    setActionError(null);
    setNotice(null);
    try {
      await action();
      setNotice(success);
      after?.();
    } catch (e) {
      setActionError(toUserMessage(e));
    } finally {
      await refresh();
      setBusy(false);
    }
  };

  const pay = () => run(() => confirmHold(holdId, studentId), 'The admission fee was received. Admission is confirmed.');
  const cancel = () => run(() => cancelHold(holdId, studentId), 'The application was cancelled.');

  return (
    <section>
      <h2>Application</h2>
      {loading && !data && <Loader />}
      {error !== undefined && !data && <ErrorMessage message={toUserMessage(error)} />}
      {data && shownStatus && (
        <div className="card">
          <p>
            Status: <HoldStatusBadge status={shownStatus} />
          </p>
          {shownStatus === 'HELD' && (
            <p>
              Time left: <CountdownTimer expiresAt={data.expiresAt} />
            </p>
          )}
          {shownStatus === 'EXPIRED' && <p className="message">This application has expired. The seat has been released for other students.</p>}
          {!isValid && <p className="message">To pay the fee or cancel, enter the birth registration number in the box at the top.</p>}
          <ErrorMessage message={actionError} />
          {notice && <p className="message message-ok">{notice}</p>}
          <div className="actions">
            <button type="button" onClick={pay} disabled={busy || !isValid || shownStatus !== 'HELD'}>
              Pay admission fee
            </button>
            <button type="button" className="secondary" onClick={cancel} disabled={busy || !isValid || shownStatus !== 'HELD'}>
              Cancel
            </button>
          </div>
        </div>
      )}
      <p>
        <button type="button" className="link" onClick={() => navigate('/me')}>
          View all my applications
        </button>
        {' · '}
        <button type="button" className="link" onClick={() => navigate('/classes')}>
          Back to the class list
        </button>
      </p>
    </section>
  );
}
