import { STUDENT_ID_LENGTH } from '../config/env';

interface Props {
  studentId: string;
  isValid: boolean;
  onChange: (value: string) => void;
}

export function StudentIdBar({ studentId, isValid, onChange }: Props) {
  const showError = studentId.length > 0 && !isValid;
  return (
    <div className="student-bar">
      <label htmlFor="student-id">Student birth registration number ({STUDENT_ID_LENGTH} digits)</label>
      <input
        id="student-id"
        inputMode="numeric"
        autoComplete="off"
        value={studentId}
        maxLength={STUDENT_ID_LENGTH}
        placeholder="e.g. 20152691234567890"
        onChange={(e) => onChange(e.target.value)}
        aria-invalid={showError}
      />
      {showError && (
        <span className="field-error" role="alert">
          The number must be exactly {STUDENT_ID_LENGTH} digits ({studentId.length}/{STUDENT_ID_LENGTH}).
        </span>
      )}
      {!studentId && <span className="hint">Enter this number to apply or to see your applications.</span>}
    </div>
  );
}
