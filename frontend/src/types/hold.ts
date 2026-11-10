export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED';

export interface Hold {
  holdId: string;
  classId: string;
  studentId?: string;
  status: HoldStatus;
  expiresAt: string;
}

export interface MyHold {
  holdId: string;
  classId: string;
  className: string;
  shift?: string | null;
  status: HoldStatus;
  expiresAt: string;
}
