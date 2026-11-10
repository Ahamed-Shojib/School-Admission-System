import { useState } from 'react';
import { SHIFTS } from '../config/shifts';
import { ErrorMessage } from './ErrorMessage';

interface Props {
  busy: boolean;
  error: string | null;
  onSubmit: (name: string, shift: string, totalSeats: number) => void;
}

export function ClassForm({ busy, error, onSubmit }: Props) {
  const [name, setName] = useState('');
  const [shift, setShift] = useState('');
  const [seats, setSeats] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  const submit = () => {
    const total = Number(seats);
    if (!name.trim()) return setLocalError('Enter a class name.');
    if (!shift) return setLocalError('Choose a shift.');
    if (!Number.isInteger(total) || total < 1) return setLocalError('Seats must be a whole number of 1 or more.');
    setLocalError(null);
    onSubmit(name.trim(), shift, total);
  };

  return (
    <div className="card form">
      <label htmlFor="class-name">Class name</label>
      <input
        id="class-name"
        value={name}
        maxLength={100}
        placeholder="e.g. Class 6"
        onChange={(e) => setName(e.target.value)}
      />
      <label htmlFor="class-shift">Shift</label>
      <select id="class-shift" value={shift} onChange={(e) => setShift(e.target.value)}>
        <option value="" disabled>
          Select a shift
        </option>
        {SHIFTS.map((s) => (
          <option key={s} value={s}>
            {s}
          </option>
        ))}
      </select>
      <label htmlFor="class-seats">Total seats</label>
      <input
        id="class-seats"
        inputMode="numeric"
        value={seats}
        placeholder="e.g. 120"
        onChange={(e) => setSeats(e.target.value.replace(/\D/g, ''))}
      />
      <ErrorMessage message={localError ?? error} />
      <button type="button" onClick={submit} disabled={busy}>
        Add class
      </button>
    </div>
  );
}
