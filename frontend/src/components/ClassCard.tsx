import type { ClassSummary } from '../types/class';

interface Props {
  item: ClassSummary;
  canApply: boolean;
  busy: boolean;
  onApply: (classId: string) => void;
}

export function ClassCard({ item, canApply, busy, onApply }: Props) {
  const full = item.availableSeats <= 0;
  return (
    <tr>
      <td>{item.name}</td>
      <td>{item.shift ?? '—'}</td>
      <td className="num">{item.totalSeats}</td>
      <td className="num">{item.heldSeats}</td>
      <td className="num">{item.confirmedSeats}</td>
      <td className="num">
        <strong>{item.availableSeats}</strong>
      </td>
      <td>
        <button
          type="button"
          onClick={() => onApply(item.id)}
          disabled={!canApply || full || busy}
          title={!canApply ? 'Enter the birth registration number at the top first' : full ? 'No seats left' : undefined}
        >
          Apply
        </button>
      </td>
    </tr>
  );
}
