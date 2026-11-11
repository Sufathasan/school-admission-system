import type { AdminApplication, ApplicationDetail, ApplicationStatusDto, ApplicationSubmissionResponse, ClassAvailability, DashboardStats, PaymentConfirmationResponse, SchoolClass, Shift } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5286';

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(options.headers ?? {}),
    },
    ...options,
  });

  if (!response.ok) {
    let message = 'Request failed';
    try {
      const payload = await response.json();
      const validationMessages = payload?.errors
        ? Object.values(payload.errors as Record<string, string[]>).flat().join(' ')
        : '';
      message = payload?.error || payload?.detail || validationMessages || payload?.title || message;
    } catch {
      message = response.statusText || message;
    }
    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  getShifts: () => request<Shift[]>('/api/shifts'),
  getClasses: () => request<SchoolClass[]>('/api/classes'),
  getClass: (classId: string) => request<SchoolClass>(`/api/classes/${classId}`),
  getAvailability: (classId?: string) =>
    request<ClassAvailability[]>(`/api/classes/availability${classId ? `?classId=${classId}` : ''}`),
  createClass: (payload: { name: string; totalSeats: number }) =>
    request<SchoolClass>('/api/classes', { method: 'POST', body: JSON.stringify(payload) }),
  createClassWithShift: (payload: { name: string; shiftId: string; totalSeats: number }) =>
    request<SchoolClass>('/api/classes/with-shift', { method: 'POST', body: JSON.stringify(payload) }),
  createShift: (payload: { name: string }) =>
    request<Shift>('/api/shifts', { method: 'POST', body: JSON.stringify(payload) }),
  createClassShift: (payload: { classId: string; shiftId: string; totalSeats: number }) =>
    request<SchoolClass>('/api/class-shifts', { method: 'POST', body: JSON.stringify(payload) }),
  updateClassShiftCapacity: (classShiftId: string, totalSeats: number) =>
    request<ClassAvailability>(`/api/class-shifts/${classShiftId}/capacity`, {
      method: 'PUT',
      body: JSON.stringify({ totalSeats }),
    }),
  createApplication: (payload: { studentName: string; birthRegistrationNumber: string; classShiftId: string }) =>
    request<ApplicationSubmissionResponse>('/api/applications', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  getStudentApplications: (studentId: string) => request<ApplicationDetail[]>(`/api/students/${studentId}/applications`),
  getApplication: (applicationId: string) => request<ApplicationDetail>(`/api/applications/${applicationId}`),
  getApplicationStatus: (applicationId: string) => request<ApplicationStatusDto>(`/api/applications/${applicationId}/status`),
  confirmPayment: (applicationId: string) =>
    request<PaymentConfirmationResponse>(`/api/payments/${applicationId}/confirm`, {
      method: 'POST',
    }),
  getAdminDashboard: () => request<DashboardStats>('/api/admin/dashboard'),
  getAdminApplications: (params?: Record<string, string>) => {
    const searchParams = new URLSearchParams();
    Object.entries(params ?? {}).forEach(([key, value]) => {
      if (value) searchParams.append(key, value);
    });
    const query = searchParams.toString();
    return request<AdminApplication[]>(`/api/admin/applications${query ? `?${query}` : ''}`);
  },
};
