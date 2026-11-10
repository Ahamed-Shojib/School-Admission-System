// Must match the shifts the backend accepts (ClassShifts.cs).
export const SHIFTS = ['Morning-Shift', 'Day-Shift'] as const;
export type Shift = (typeof SHIFTS)[number];
