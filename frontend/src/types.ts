export type ApplicationStatus = 'Pending' | 'Confirmed' | 'Expired';

export interface Shift {
  id: string;
  name: string;
}

export interface SchoolClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
  createdAtUtc: string;
}

export interface ClassAvailability {
  classShiftId: string;
  classId: string;
  className: string;
  shiftId: string;
  shiftName: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface ApplicationDetail {
  id: string;
  studentId: string;
  studentName: string;
  birthRegistrationNumber: string;
  classShiftId: string;
  className: string;
  shiftName: string;
  status: ApplicationStatus;
  createdAtUtc: string;
  holdExpiresAtUtc: string;
  confirmedAtUtc?: string | null;
  expiredAtUtc?: string | null;
}

export interface AdminApplication {
  id: string;
  studentId: string;
  studentName: string;
  birthRegistrationNumber: string;
  classId: string;
  className: string;
  shiftId: string;
  shiftName: string;
  status: ApplicationStatus;
  createdAtUtc: string;
  holdExpiresAtUtc: string;
  confirmedAtUtc?: string | null;
  expiredAtUtc?: string | null;
}

export interface ApplicationStatusDto {
  applicationId: string;
  status: ApplicationStatus;
  createdAtUtc: string;
  holdExpiresAtUtc: string;
  isExpired: boolean;
}

export interface DashboardStats {
  totalClasses: number;
  totalSeats: number;
  heldSeats: number;
  confirmedAdmissions: number;
  availableSeats: number;
  pendingApplications: number;
  expiredApplications: number;
}

export interface PaymentConfirmationResponse {
  paymentId: string;
  applicationId: string;
  amount: number;
  status: string;
  confirmedAtUtc: string;
}

export interface ApplicationSubmissionResponse {
  applicationId: string;
  studentId: string;
  classShiftId: string;
  className: string;
  shiftName: string;
  status: string;
  holdExpiresAtUtc: string;
}
