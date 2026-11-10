import { useState } from 'react';
import { listClasses } from '../api/classesApi';
import { applyToClass } from '../api/holdsApi';
import { ClassCard } from '../components/ClassCard';
import { ErrorMessage } from '../components/ErrorMessage';
import { Loader } from '../components/Loader';
import { CLASS_POLL_MS } from '../config/env';
import { usePolling } from '../hooks/usePolling';
import { toUserMessage } from '../utils/errorMessages';

interface Props {
  studentId: string;
  isValid: boolean;
  navigate: (to: string) => void;
}

export function ClassesPage({ studentId, isValid, navigate }: Props) {
  const { data, error, loading, refresh } = usePolling(listClasses, CLASS_POLL_MS, 'classes');
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const apply = async (classId: string) => {
    setBusyId(classId);
    setActionError(null);
    try {
      const hold = await applyToClass(classId, studentId);
      navigate(`/holds/${hold.holdId}`);
    } catch (e) {
      setActionError(toUserMessage(e));
      void refresh(); // seat counts may have changed under us
    } finally {
      setBusyId(null);
    }
  };

  return (
    <section>
      <h2>Classes</h2>
      {!isValid && <p className="message">To apply, first enter the student's 17-digit birth registration number in the box at the top.</p>}
      <ErrorMessage message={actionError} />
      {loading && !data && <Loader />}
      {error !== undefined && <ErrorMessage message={toUserMessage(error)} />}
      {data && data.length === 0 && <p>No classes have been added yet.</p>}
      {data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Class</th>
                <th>Shift</th>
                <th>Total seats</th>
                <th>On hold</th>
                <th>Confirmed</th>
                <th>Available</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {data.map((item) => (
                <ClassCard key={item.id} item={item} canApply={isValid} busy={busyId !== null} onApply={apply} />
              ))}
            </tbody>
          </table>
        </div>
      )}
      <p className="hint">Seat numbers refresh automatically every few seconds.</p>
    </section>
  );
}
