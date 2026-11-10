export interface ClassSummary {
  id: string;
  name: string;
  shift?: string | null;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}
