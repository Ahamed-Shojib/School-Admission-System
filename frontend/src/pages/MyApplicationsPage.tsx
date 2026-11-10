import { listMyHolds } from '../api/holdsApi';
import { CountdownTimer } from '../components/CountdownTimer';
import { ErrorMessage } from '../components/ErrorMessage';
import { HoldStatusBadge } from '../components/HoldStatusBadge';
import { Loader } from '../components/Loader';
import { CLASS_POLL_MS } from '../config/env';
import { usePolling } from '../hooks/usePolling';
import type { HoldStatus } from '../types/hold';
import { toUserMessage } from '../utils/errorMessages';
import { effectiveStatus } from '../utils/formatTime';

interface Props {
  studentId: string;
  isValid: boolean;
  navigate: (to: string) => void;
}

export function MyApplicationsPage({ studentId, isValid, navigate }: Props) {
  const { data, error, loading } = usePolling(() => listMyHolds(studentId), CLASS_POLL_MS, studentId, isValid);

  return (
    <section>
      <h2>My applications</h2>
      {!isValid && <p className="message">To see your applications, enter the student's 17-digit birth registration number in the box at the top.</p>}
      {isValid && loading && !data && <Loader />}
      {isValid && error !== undefined && <ErrorMessage message={toUserMessage(error)} />}
      {isValid && data && data.length === 0 && <p>This student has no applications yet.</p>}
      {isValid && data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th>Shift</th>
                <th>Status</th>
                <th>Time left</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.map((h) => {
                const status: HoldStatus = effectiveStatus(h.status, h.expiresAt, Date.now());
                return (
                  <tr key={h.holdId}>
                    <td>{h.className}</td>
                    <td>{h.shift ?? '—'}</td>
                    <td>
                      <HoldStatusBadge status={status} />
                    </td>
                    <td>{status === 'HELD' ? <CountdownTimer expiresAt={h.expiresAt} /> : '—'}</td>
                    <td>
                      <button type="button" onClick={() => navigate(`/holds/${h.holdId}`)}>
                        Details
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
