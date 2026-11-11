import { BrowserRouter, Link, Route, Routes, useParams } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { api } from './services/api';
import type { AdminApplication, ApplicationDetail, ApplicationSubmissionResponse, ClassAvailability, DashboardStats, PaymentConfirmationResponse, SchoolClass, Shift } from './types';

const STORAGE_KEY = 'school-admission-student-id';

function formatDate(value: string | undefined): string {
  if (!value) return '—';
  return new Date(value).toLocaleString();
}

function formatCountdown(expiresAtUtc: string): string {
  const target = new Date(expiresAtUtc).getTime();
  const now = Date.now();
  const diff = Math.max(0, target - now);
  const totalSeconds = Math.floor(diff / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}m ${seconds.toString().padStart(2, '0')}s`;
}

function HomePage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      try {
        setLoading(true);
        const dashboard = await api.getAdminDashboard();
        setStats(dashboard);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unable to load dashboard data');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  return (
    <div className="space-y-8">
      <section className="rounded-3xl bg-slate-900 p-8 text-white shadow-xl">
        <p className="text-sm uppercase tracking-[0.3em] text-sky-300">School admission portal</p>
        <h1 className="mt-4 text-4xl font-bold">Apply for a seat with confidence.</h1>
        <p className="mt-4 max-w-2xl text-slate-300">
          Students can view available classes, reserve a seat, pay to confirm admission, and track their application status in real time.
        </p>
        <div className="mt-6 flex flex-wrap gap-4">
          <Link to="/classes" className="rounded-lg bg-sky-500 px-5 py-3 font-semibold text-white hover:bg-sky-400">View Classes</Link>
          <Link to="/apply" className="rounded-lg border border-slate-600 bg-slate-800 px-5 py-3 font-semibold text-slate-100 hover:border-sky-300">Apply Now</Link>
        </div>
      </section>

      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        {loading ? (
          <p>Loading overview...</p>
        ) : error ? (
          <p className="text-red-600">{error}</p>
        ) : stats ? (
          <>
            <StatCard label="Total Classes" value={stats.totalClasses.toString()} />
            <StatCard label="Total Seats" value={stats.totalSeats.toString()} />
            <StatCard label="Held Seats" value={stats.heldSeats.toString()} />
            <StatCard label="Available Seats" value={stats.availableSeats.toString()} />
          </>
        ) : null}
      </section>
    </div>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
      <p className="text-sm text-slate-500">{label}</p>
      <p className="mt-2 text-3xl font-bold text-slate-900">{value}</p>
    </div>
  );
}

function ClassesPage() {
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [availability, setAvailability] = useState<ClassAvailability[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      try {
        const [classData, availabilityData] = await Promise.all([api.getClasses(), api.getAvailability()]);
        if (active) {
          setClasses(classData);
          setAvailability(availabilityData);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load class availability.');
      } finally {
        if (active) setLoading(false);
      }
    };

    void refresh();
    const intervalId = window.setInterval(() => void refresh(), 5000);
    return () => {
      active = false;
      window.clearInterval(intervalId);
    };
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-semibold text-slate-900">Available classes</h2>
        <Link to="/apply" className="rounded-lg bg-slate-900 px-4 py-2 text-white">Apply now</Link>
      </div>

      {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>}
      {loading && <p role="status">Loading classes...</p>}
      <div className="grid gap-4 lg:grid-cols-2">
        {!loading && classes.length === 0 && !error && <p className="rounded-xl border border-dashed p-6 text-slate-500">No classes are available yet.</p>}
        {classes.map((schoolClass) => (
          <div key={schoolClass.id} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-xl font-semibold text-slate-900">{schoolClass.name}</h3>
                <p className="mt-1 text-sm text-slate-500">Total seats: {schoolClass.totalSeats}</p>
              </div>
              <span className="rounded-full bg-green-100 px-2.5 py-1 text-xs font-medium text-green-700">
                {schoolClass.availableSeats} available
              </span>
            </div>
            <div className="mt-4 grid gap-2 text-sm text-slate-600">
              <p>Held seats: {schoolClass.heldSeats}</p>
              <p>Confirmed: {schoolClass.confirmedSeats}</p>
            </div>
            <div className="mt-5 space-y-2">
              {availability.filter((item) => item.classId === schoolClass.id).map((item) => (
                <div key={`${item.classId}-${item.shiftId}`} className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm">
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-slate-700">{item.shiftName}</span>
                    <span className="text-slate-500">{item.availableSeats} / {item.totalSeats} available</span>
                  </div>
                  <p className="mt-1 text-xs text-slate-500">Held {item.heldSeats} · Confirmed {item.confirmedSeats}</p>
                </div>
              ))}
            </div>
            <Link to={`/classes/${schoolClass.id}`} className="mt-4 inline-flex text-sm font-semibold text-sky-700 hover:text-sky-900">Class details</Link>
          </div>
        ))}
      </div>
    </div>
  );
}

function ClassDetailsPage() {
  const { classId = '' } = useParams();
  const [schoolClass, setSchoolClass] = useState<SchoolClass | null>(null);
  const [availability, setAvailability] = useState<ClassAvailability[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      try {
        const [classResult, availabilityResult] = await Promise.all([
          api.getClass(classId),
          api.getAvailability(classId),
        ]);
        if (active) {
          setSchoolClass(classResult);
          setAvailability(availabilityResult);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load class details.');
      } finally {
        if (active) setLoading(false);
      }
    };
    void refresh();
    const intervalId = window.setInterval(() => void refresh(), 5000);
    return () => {
      active = false;
      window.clearInterval(intervalId);
    };
  }, [classId]);

  if (loading) return <p role="status">Loading class details...</p>;
  if (error) return <p role="alert" className="rounded-lg bg-red-50 p-3 text-red-700">{error}</p>;
  if (!schoolClass) return <p>Class not found.</p>;

  return (
    <section className="space-y-5 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <div>
        <p className="text-sm text-slate-500">Class details</p>
        <h2 className="text-2xl font-semibold">{schoolClass.name}</h2>
        <p className="mt-2 text-sm text-slate-600">Total {schoolClass.totalSeats} · Held {schoolClass.heldSeats} · Confirmed {schoolClass.confirmedSeats} · Available {schoolClass.availableSeats}</p>
      </div>
      <div className="grid gap-3 md:grid-cols-2">
        {availability.map((item) => (
          <div key={item.classShiftId} className="rounded-xl border border-slate-200 p-4">
            <h3 className="font-semibold">{item.shiftName}</h3>
            <p className="mt-2 text-sm text-slate-600">Total {item.totalSeats} · Held {item.heldSeats} · Confirmed {item.confirmedSeats} · Available {item.availableSeats}</p>
          </div>
        ))}
      </div>
      <Link to="/apply" className="inline-flex rounded-lg bg-slate-900 px-4 py-2 text-white">Apply for admission</Link>
    </section>
  );
}

function ApplicationFormPage() {
  const [availability, setAvailability] = useState<ClassAvailability[]>([]);
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [formState, setFormState] = useState({ studentName: '', birthRegistrationNumber: '', classShiftId: '' });
  const [submitted, setSubmitted] = useState<ApplicationSubmissionResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [loading, setLoading] = useState(true);
  const [clock, setClock] = useState(0);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      try {
        const [classData, availabilityData] = await Promise.all([api.getClasses(), api.getAvailability()]);
        if (active) {
          setClasses(classData);
          setAvailability(availabilityData);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load admission options.');
      } finally {
        if (active) setLoading(false);
      }
    };
    void refresh();
    const refreshId = window.setInterval(() => void refresh(), 5000);
    const initialClockId = window.setTimeout(() => setClock(Date.now()), 0);
    const clockId = window.setInterval(() => setClock(Date.now()), 1000);
    return () => {
      active = false;
      window.clearInterval(refreshId);
      window.clearTimeout(initialClockId);
      window.clearInterval(clockId);
    };
  }, []);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError(null);
    setSubmitted(null);

    if (!formState.studentName || !formState.birthRegistrationNumber || !formState.classShiftId) {
      setError('Please complete all fields.');
      return;
    }

    if (!/^[0-9]{17}$/.test(formState.birthRegistrationNumber)) {
      setError('Birth Registration Number must contain exactly 17 digits.');
      return;
    }

    setSubmitting(true);

    try {
      const result = await api.createApplication({
        studentName: formState.studentName,
        birthRegistrationNumber: formState.birthRegistrationNumber,
        classShiftId: formState.classShiftId,
      });

      localStorage.setItem(STORAGE_KEY, result.studentId);
      setSubmitted(result);
      setFormState({ studentName: '', birthRegistrationNumber: '', classShiftId: '' });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Submission failed');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="grid gap-8 lg:grid-cols-[1.3fr_0.7fr]">
      <form onSubmit={handleSubmit} className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="text-2xl font-semibold text-slate-900">Admission application</h2>
        <div className="mt-5 space-y-5">
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Full name</span>
            <input
              required
              minLength={2}
              maxLength={200}
              className="w-full rounded-xl border border-slate-300 px-3 py-2 outline-none ring-0 focus:border-sky-500"
              value={formState.studentName}
              onChange={(event) => setFormState((state) => ({ ...state, studentName: event.target.value }))}
            />
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Birth Registration Number</span>
            <input
              required
              inputMode="numeric"
              maxLength={17}
              pattern="[0-9]{17}"
              aria-describedby="birth-registration-help"
              className="w-full rounded-xl border border-slate-300 px-3 py-2 outline-none ring-0 focus:border-sky-500"
              value={formState.birthRegistrationNumber}
              onChange={(event) => setFormState((state) => ({ ...state, birthRegistrationNumber: event.target.value }))}
            />
            <span id="birth-registration-help" className="mt-1 block text-xs text-slate-500">Enter exactly 17 digits.</span>
          </label>

          <label className="block">
            <span className="mb-1 block text-sm font-medium text-slate-700">Class and shift</span>
            <select
              required
              className="w-full rounded-xl border border-slate-300 px-3 py-2 outline-none focus:border-sky-500"
              value={formState.classShiftId}
              onChange={(event) => setFormState((state) => ({ ...state, classShiftId: event.target.value }))}
            >
              <option value="">Select a class and shift</option>
              {availability.filter((option) => option.availableSeats > 0).map((option) => (
                <option key={option.classShiftId} value={option.classShiftId}>
                  {option.className} / {option.shiftName} ({option.availableSeats} of {option.totalSeats} seats available)
                </option>
              ))}
            </select>
          </label>

          {loading && <p role="status" className="text-sm text-slate-500">Loading classes and shifts...</p>}
          {submitted && (
            <div role="status" className="space-y-2 rounded-lg bg-green-50 p-4 text-sm text-green-800">
              <p className="font-semibold">Application submitted. Status: {submitted.status}</p>
              <p>Application ID: {submitted.applicationId}</p>
              <p>Hold expires: {formatDate(submitted.holdExpiresAtUtc)}</p>
              <p>Time remaining: {Date.parse(submitted.holdExpiresAtUtc) <= clock ? 'Expired' : formatCountdown(submitted.holdExpiresAtUtc)}</p>
              {Date.parse(submitted.holdExpiresAtUtc) > clock && <Link className="inline-flex font-semibold underline" to={`/payment/${submitted.applicationId}`}>Continue to payment</Link>}
              <p><Link className="underline" to="/applications">View my applications</Link></p>
            </div>
          )}
          {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>}

          <button type="submit" disabled={submitting || loading || availability.length === 0} className="w-full rounded-xl bg-slate-900 px-4 py-3 font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-400">
            {submitting ? 'Submitting...' : 'Submit Application'}
          </button>
        </div>
      </form>

      <aside className="rounded-2xl border border-slate-200 bg-slate-50 p-6 shadow-sm">
        <h3 className="text-lg font-semibold text-slate-900">Seat availability</h3>
        <div className="mt-4 space-y-3">
          {classes.map((schoolClass) => (
            <div key={schoolClass.id} className="rounded-xl border border-slate-200 bg-white p-3">
              <div className="flex items-center justify-between">
                <p className="font-semibold text-slate-800">{schoolClass.name}</p>
                <span className="text-xs text-slate-500">{schoolClass.availableSeats} left</span>
              </div>
              <p className="mt-1 text-sm text-slate-500">{schoolClass.availableSeats} / {schoolClass.totalSeats} seats available</p>
            </div>
          ))}
        </div>
      </aside>
    </div>
  );
}

function MyApplicationsPage() {
  const [studentId, setStudentId] = useState<string>(localStorage.getItem(STORAGE_KEY) ?? '');
  const [applications, setApplications] = useState<ApplicationDetail[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [clock, setClock] = useState(0);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      const storedId = localStorage.getItem(STORAGE_KEY);
      if (!storedId) return;
      try {
        const result = await api.getStudentApplications(storedId);
        if (active) {
          setApplications(result);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load applications.');
      } finally {
        if (active) setLoading(false);
      }
    };
    void refresh();
    const refreshId = window.setInterval(() => void refresh(), 5000);
    const initialClockId = window.setTimeout(() => setClock(Date.now()), 0);
    const clockId = window.setInterval(() => setClock(Date.now()), 1000);
    return () => {
      active = false;
      window.clearInterval(refreshId);
      window.clearTimeout(initialClockId);
      window.clearInterval(clockId);
    };
  }, []);

  const fetchApplications = async (nextStudentId: string) => {
    if (!nextStudentId.trim()) {
      setError('Enter your student ID to look up applications.');
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const result = await api.getStudentApplications(nextStudentId.trim());
      setApplications(result);
      localStorage.setItem(STORAGE_KEY, nextStudentId.trim());
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <h2 className="text-2xl font-semibold text-slate-900">My applications</h2>
        <div className="mt-4 flex flex-col gap-3 md:flex-row">
          <input
            value={studentId}
            onChange={(event) => setStudentId(event.target.value)}
            className="flex-1 rounded-xl border border-slate-300 px-3 py-2"
            placeholder="Student ID"
          />
          <button onClick={() => void fetchApplications(studentId).catch((err: unknown) => setError(err instanceof Error ? err.message : 'Unable to load applications.'))} className="rounded-xl bg-slate-900 px-4 py-2 text-white">Load</button>
        </div>
      </div>

      {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>}
      {loading ? (
        <p role="status">Loading applications...</p>
      ) : applications.length === 0 ? (
        <p className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-6 text-slate-500">No applications found.</p>
      ) : (
        <div className="space-y-4">
          {applications.map((application) => (
            <div key={application.id} className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
              <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <div>
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Application {application.id}</p>
                  <h3 className="mt-2 text-xl font-semibold text-slate-900">{application.className} / {application.shiftName}</h3>
                </div>
                <span className={`inline-flex rounded-full px-3 py-1 text-sm font-medium ${application.status === 'Confirmed' ? 'bg-green-100 text-green-700' : application.status === 'Expired' ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-700'}`}>
                  {application.status}
                </span>
              </div>
              <div className="mt-4 grid gap-3 text-sm text-slate-600 md:grid-cols-3">
                <p>Created: {formatDate(application.createdAtUtc)}</p>
                <p>Hold expires: {formatDate(application.holdExpiresAtUtc)}</p>
                <p>Countdown: {application.status === 'Pending' && Date.parse(application.holdExpiresAtUtc) > clock ? formatCountdown(application.holdExpiresAtUtc) : application.status === 'Pending' ? 'Expired' : application.status}</p>
              </div>
              {application.status === 'Pending' && Date.parse(application.holdExpiresAtUtc) > clock && (
                <Link to={`/payment/${application.id}`} className="mt-5 inline-flex rounded-lg bg-sky-600 px-4 py-2 text-sm font-semibold text-white hover:bg-sky-500">Pay and confirm</Link>
              )}
              <Link to={`/applications/${application.id}/status`} className="ml-3 mt-5 inline-flex text-sm font-semibold text-slate-700 underline">Status details</Link>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function PaymentPage() {
  const { applicationId } = useParams();
  const [application, setApplication] = useState<ApplicationDetail | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isConfirming, setIsConfirming] = useState(false);
  const [loading, setLoading] = useState(true);
  const [clock, setClock] = useState(0);

  useEffect(() => {
    if (!applicationId) return;
    let active = true;
    const refresh = async () => {
      try {
        const result = await api.getApplication(applicationId);
        if (active) {
          setApplication(result);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load application.');
      } finally {
        if (active) setLoading(false);
      }
    };
    void refresh();
    const refreshId = window.setInterval(() => void refresh(), 5000);
    const initialClockId = window.setTimeout(() => setClock(Date.now()), 0);
    const clockId = window.setInterval(() => setClock(Date.now()), 1000);
    return () => {
      active = false;
      window.clearInterval(refreshId);
      window.clearTimeout(initialClockId);
      window.clearInterval(clockId);
    };
  }, [applicationId]);

  const handleConfirm = async () => {
    if (!applicationId || !application || Date.parse(application.holdExpiresAtUtc) <= clock) return;
    setIsConfirming(true);
    setError(null);
    setMessage(null);
    try {
      const response: PaymentConfirmationResponse = await api.confirmPayment(applicationId);
      setMessage(`Payment confirmed successfully at ${formatDate(response.confirmedAtUtc)}`);
      const updated = application ? { ...application, status: 'Confirmed' as const } : null;
      setApplication(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to confirm payment');
    } finally {
      setIsConfirming(false);
    }
  };

  if (loading) return <p role="status">Loading application...</p>;
  if (!application) return <p role="alert" className="rounded-lg bg-red-50 p-3 text-red-700">{error ?? 'Application not found.'}</p>;
  const holdIsActive = application.status === 'Pending' && Date.parse(application.holdExpiresAtUtc) > clock;

  return (
    <div className="max-w-2xl rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <h2 className="text-2xl font-semibold text-slate-900">Payment confirmation</h2>
      <div className="mt-5 space-y-3 text-slate-700">
        <p><strong>Class:</strong> {application.className}</p>
        <p><strong>Shift:</strong> {application.shiftName}</p>
        <p><strong>Status:</strong> {application.status}</p>
        <p><strong>Hold expires:</strong> {formatDate(application.holdExpiresAtUtc)}</p>
        {application.status === 'Pending' && <p><strong>Time remaining:</strong> {holdIsActive ? formatCountdown(application.holdExpiresAtUtc) : 'Expired'}</p>}
      </div>
      {message && <p role="status" className="mt-5 rounded-lg bg-green-50 p-3 text-sm text-green-700">{message}</p>}
      {error && <p role="alert" className="mt-5 rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>}
      {!holdIsActive && application.status === 'Pending' && <p className="mt-5 text-sm text-red-700">This hold has expired. Payment is disabled.</p>}
      <button onClick={() => void handleConfirm()} disabled={isConfirming || !holdIsActive} className="mt-6 rounded-xl bg-emerald-600 px-4 py-3 font-semibold text-white disabled:cursor-not-allowed disabled:bg-slate-300">
        {isConfirming ? 'Confirming...' : 'Confirm Payment'}
      </button>
    </div>
  );
}

function ApplicationStatusPage() {
  const { applicationId = '' } = useParams();
  const [application, setApplication] = useState<ApplicationDetail | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [clock, setClock] = useState(0);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      try {
        const result = await api.getApplication(applicationId);
        if (active) {
          setApplication(result);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load application status.');
      }
    };
    void refresh();
    const intervalId = window.setInterval(() => void refresh(), 5000);
    const initialClockId = window.setTimeout(() => setClock(Date.now()), 0);
    const clockId = window.setInterval(() => setClock(Date.now()), 1000);
    return () => {
      active = false;
      window.clearInterval(intervalId);
      window.clearTimeout(initialClockId);
      window.clearInterval(clockId);
    };
  }, [applicationId]);

  if (error) return <p role="alert" className="rounded-lg bg-red-50 p-3 text-red-700">{error}</p>;
  if (!application) return <p role="status">Loading application status...</p>;

  return (
    <section className="max-w-2xl space-y-3 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
      <h2 className="text-2xl font-semibold">Application status</h2>
      <p>Application ID: {application.id}</p>
      <p>Student: {application.studentName}</p>
      <p>Class / shift: {application.className} / {application.shiftName}</p>
      <p>Status: <strong>{application.status}</strong></p>
      <p>Submitted: {formatDate(application.createdAtUtc)}</p>
      <p>Hold expires: {formatDate(application.holdExpiresAtUtc)}</p>
      {application.status === 'Pending' && Date.parse(application.holdExpiresAtUtc) > clock &&
        <Link className="inline-flex rounded-lg bg-sky-600 px-4 py-2 text-white" to={`/payment/${application.id}`}>Continue to payment</Link>}
    </section>
  );
}

function AdminDashboardPage() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [applications, setApplications] = useState<AdminApplication[]>([]);
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [availability, setAvailability] = useState<ClassAvailability[]>([]);
  const [classFilter, setClassFilter] = useState('');
  const [shiftFilter, setShiftFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [className, setClassName] = useState('');
  const [classShiftId, setClassShiftId] = useState('');
  const [classCapacity, setClassCapacity] = useState('30');
  const [newShiftName, setNewShiftName] = useState('');
  const [targetClassId, setTargetClassId] = useState('');
  const [targetShiftId, setTargetShiftId] = useState('');
  const [targetCapacity, setTargetCapacity] = useState('30');
  const [capacityValues, setCapacityValues] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const refresh = async () => {
      try {
        const [dashboard, appData, classData, shiftData, seatData] = await Promise.all([
          api.getAdminDashboard(),
          api.getAdminApplications({
            classId: classFilter,
            shiftId: shiftFilter,
            status: statusFilter,
            search,
          }),
          api.getClasses(),
          api.getShifts(),
          api.getAvailability(),
        ]);
        if (active) {
          setStats(dashboard);
          setApplications(appData);
          setClasses(classData);
          setShifts(shiftData);
          setAvailability(seatData);
          setError(null);
        }
      } catch (err) {
        if (active) setError(err instanceof Error ? err.message : 'Unable to load admin data.');
      } finally {
        if (active) setLoading(false);
      }
    };
    void refresh();
    const intervalId = window.setInterval(() => void refresh(), 15000);
    return () => {
      active = false;
      window.clearInterval(intervalId);
    };
  }, [classFilter, search, shiftFilter, statusFilter]);

  const refreshAvailability = async () => {
    const [dashboard, appData, classData, shiftData, seatData] = await Promise.all([
      api.getAdminDashboard(),
      api.getAdminApplications({ classId: classFilter, shiftId: shiftFilter, status: statusFilter, search }),
      api.getClasses(),
      api.getShifts(),
      api.getAvailability(),
    ]);
    setStats(dashboard);
    setApplications(appData);
    setClasses(classData);
    setShifts(shiftData);
    setAvailability(seatData);
  };

  const createClassWithShift = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!classShiftId) {
      setError('Select a shift to configure for this class.');
      return;
    }
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await api.createClassWithShift({
        name: className,
        shiftId: classShiftId,
        totalSeats: Number(classCapacity),
      });
      setClassName('');
      setNotice('Class created and its first shift capacity configured.');
      await refreshAvailability();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to create the class.');
    } finally {
      setSaving(false);
    }
  };

  const createShift = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await api.createShift({ name: newShiftName });
      setNewShiftName('');
      setNotice('Shift created.');
      await refreshAvailability();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to create the shift.');
    } finally {
      setSaving(false);
    }
  };

  const addClassShift = async (event: React.FormEvent) => {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await api.createClassShift({
        classId: targetClassId,
        shiftId: targetShiftId,
        totalSeats: Number(targetCapacity),
      });
      setNotice('Shift added to the class.');
      await refreshAvailability();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to add this shift.');
    } finally {
      setSaving(false);
    }
  };

  const updateCapacity = async (item: ClassAvailability) => {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      await api.updateClassShiftCapacity(item.classShiftId, Number(capacityValues[item.classShiftId] ?? item.totalSeats));
      setNotice(`Seat capacity updated for ${item.className} / ${item.shiftName}.`);
      await refreshAvailability();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to update seat capacity.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="grid items-start gap-6 lg:grid-cols-[220px_minmax(0,1fr)]">
      <aside className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm lg:sticky lg:top-6">
        <p className="px-3 pb-3 text-xs font-semibold uppercase tracking-wider text-slate-500">Administration</p>
        <nav aria-label="Admin dashboard sections" className="flex gap-2 overflow-x-auto lg:flex-col">
          <a href="#dashboard-overview" className="whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">Overview</a>
          <a href="#class-management" className="whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">Classes and shifts</a>
          <a href="#class-capacity" className="whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">Seat capacity</a>
          <a href="#application-management" className="whitespace-nowrap rounded-lg px-3 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100">Applications</a>
        </nav>
      </aside>
      <div className="min-w-0 space-y-6">
      <h2 className="text-2xl font-semibold text-slate-900">Admin dashboard</h2>
      {error && <p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>}
      {notice && <p role="status" className="rounded-lg bg-green-50 p-3 text-sm text-green-700">{notice}</p>}
      {loading && <p role="status">Loading dashboard data...</p>}
      {stats && (
        <div id="dashboard-overview" className="grid gap-4 scroll-mt-6 sm:grid-cols-2 xl:grid-cols-4">
          <StatCard label="Total Classes" value={stats.totalClasses.toString()} />
          <StatCard label="Total Seats" value={stats.totalSeats.toString()} />
          <StatCard label="Held Seats" value={stats.heldSeats.toString()} />
          <StatCard label="Confirmed Admissions" value={stats.confirmedAdmissions.toString()} />
          <StatCard label="Available Seats" value={stats.availableSeats.toString()} />
          <StatCard label="Pending Applications" value={stats.pendingApplications.toString()} />
          <StatCard label="Expired Applications" value={stats.expiredApplications.toString()} />
        </div>
      )}

      <section id="class-management" className="grid scroll-mt-6 gap-4 xl:grid-cols-2">
        <form onSubmit={(event) => void createClassWithShift(event)} className="space-y-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="font-semibold">Create a class</h3>
          <input required minLength={2} maxLength={120} value={className} onChange={(event) => setClassName(event.target.value)} placeholder="Class name" className="w-full rounded-xl border border-slate-300 px-3 py-2" />
          <div className="grid gap-3 sm:grid-cols-2">
            <select required value={classShiftId} onChange={(event) => setClassShiftId(event.target.value)} className="rounded-xl border border-slate-300 px-3 py-2">
              <option value="">First shift</option>
              {shifts.map((shift) => <option key={shift.id} value={shift.id}>{shift.name}</option>)}
            </select>
            <input required type="number" min={1} value={classCapacity} onChange={(event) => setClassCapacity(event.target.value)} aria-label="Initial shift capacity" className="rounded-xl border border-slate-300 px-3 py-2" />
          </div>
          <button disabled={saving || shifts.length === 0} className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:bg-slate-400">{saving ? 'Saving...' : 'Create class'}</button>
        </form>

        <form onSubmit={(event) => void createShift(event)} className="space-y-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
          <h3 className="font-semibold">Create a shift</h3>
          <input required minLength={2} maxLength={60} value={newShiftName} onChange={(event) => setNewShiftName(event.target.value)} placeholder="Shift name" className="w-full rounded-xl border border-slate-300 px-3 py-2" />
          <button disabled={saving} className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:bg-slate-400">{saving ? 'Saving...' : 'Create shift'}</button>
        </form>

        <form onSubmit={(event) => void addClassShift(event)} className="space-y-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm xl:col-span-2">
          <h3 className="font-semibold">Add a shift and capacity to a class</h3>
          <div className="grid gap-3 sm:grid-cols-3">
            <select required value={targetClassId} onChange={(event) => setTargetClassId(event.target.value)} className="rounded-xl border border-slate-300 px-3 py-2">
              <option value="">Select class</option>
              {classes.map((schoolClass) => <option key={schoolClass.id} value={schoolClass.id}>{schoolClass.name}</option>)}
            </select>
            <select required value={targetShiftId} onChange={(event) => setTargetShiftId(event.target.value)} className="rounded-xl border border-slate-300 px-3 py-2">
              <option value="">Select shift</option>
              {shifts.map((shift) => <option key={shift.id} value={shift.id}>{shift.name}</option>)}
            </select>
            <input required type="number" min={1} value={targetCapacity} onChange={(event) => setTargetCapacity(event.target.value)} aria-label="Shift seat capacity" className="rounded-xl border border-slate-300 px-3 py-2" />
          </div>
          <button disabled={saving || classes.length === 0 || shifts.length === 0} className="rounded-lg bg-slate-900 px-4 py-2 text-white disabled:bg-slate-400">{saving ? 'Saving...' : 'Add shift to class'}</button>
        </form>
      </section>

      <section id="class-capacity" className="scroll-mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <h3 className="mb-4 font-semibold">Class seat capacity</h3>
        <div className="overflow-x-auto">
          <table className="min-w-full text-left text-sm text-slate-700">
            <thead><tr className="border-b bg-slate-50 text-xs uppercase text-slate-500"><th className="px-3 py-3">Class / shift</th><th className="px-3 py-3">Total</th><th className="px-3 py-3">Held</th><th className="px-3 py-3">Confirmed</th><th className="px-3 py-3">Available</th><th className="px-3 py-3">Capacity action</th></tr></thead>
            <tbody>
              {availability.map((item) => (
                <tr key={item.classShiftId} className="border-b border-slate-100">
                  <td className="px-3 py-3">{item.className} / {item.shiftName}</td>
                  <td className="px-3 py-3">{item.totalSeats}</td>
                  <td className="px-3 py-3">{item.heldSeats}</td>
                  <td className="px-3 py-3">{item.confirmedSeats}</td>
                  <td className="px-3 py-3">{item.availableSeats}</td>
                  <td className="px-3 py-3">
                    <div className="flex gap-2">
                      <input type="number" min={item.heldSeats + item.confirmedSeats} value={capacityValues[item.classShiftId] ?? String(item.totalSeats)} onChange={(event) => setCapacityValues((values) => ({ ...values, [item.classShiftId]: event.target.value }))} aria-label={`Capacity for ${item.className} ${item.shiftName}`} className="w-24 rounded-lg border px-2 py-1" />
                      <button disabled={saving} onClick={() => void updateCapacity(item)} className="rounded-lg border px-3 py-1 disabled:opacity-50">Save</button>
                    </div>
                  </td>
                </tr>
              ))}
              {availability.length === 0 && <tr><td colSpan={6} className="px-3 py-5 text-slate-500">No class-shift capacities are configured.</td></tr>}
            </tbody>
          </table>
        </div>
      </section>

      <div id="application-management" className="scroll-mt-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
        <h3 className="font-semibold">Admission applications</h3>
        <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search student or registration number" className="rounded-xl border border-slate-300 px-3 py-2" />
          <select value={classFilter} onChange={(event) => setClassFilter(event.target.value)} className="rounded-xl border border-slate-300 px-3 py-2">
            <option value="">All classes</option>
            {classes.map((schoolClass) => <option key={schoolClass.id} value={schoolClass.id}>{schoolClass.name}</option>)}
          </select>
          <select value={shiftFilter} onChange={(event) => setShiftFilter(event.target.value)} className="rounded-xl border border-slate-300 px-3 py-2">
            <option value="">All shifts</option>
            {shifts.map((shift) => <option key={shift.id} value={shift.id}>{shift.name}</option>)}
          </select>
          <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} className="rounded-xl border border-slate-300 px-3 py-2">
            <option value="">All statuses</option>
            <option value="Pending">Pending</option>
            <option value="Confirmed">Confirmed</option>
            <option value="Expired">Expired</option>
          </select>
        </div>

        <div className="mt-4 overflow-x-auto">
          <table className="min-w-full text-left text-sm text-slate-700">
            <thead>
              <tr className="border-b border-slate-200 bg-slate-50 text-xs uppercase tracking-[0.12em] text-slate-500">
                <th className="px-3 py-3">Student</th>
                <th className="px-3 py-3">Birth Registration No.</th>
                <th className="px-3 py-3">Class</th>
                <th className="px-3 py-3">Shift</th>
                <th className="px-3 py-3">Status</th>
                <th className="px-3 py-3">Submitted</th>
                <th className="px-3 py-3">Hold expires</th>
              </tr>
            </thead>
            <tbody>
              {applications.map((item) => (
                <tr key={item.id} className="border-b border-slate-100">
                  <td className="px-3 py-3">{item.studentName}</td>
                  <td className="px-3 py-3">{item.birthRegistrationNumber}</td>
                  <td className="px-3 py-3">{item.className}</td>
                  <td className="px-3 py-3">{item.shiftName}</td>
                  <td className="px-3 py-3">
                    <span className={`rounded-full px-2 py-1 text-xs font-medium ${item.status === 'Confirmed' ? 'bg-green-100 text-green-700' : item.status === 'Expired' ? 'bg-red-100 text-red-700' : 'bg-amber-100 text-amber-700'}`}>
                      {item.status}
                    </span>
                  </td>
                  <td className="px-3 py-3">{formatDate(item.createdAtUtc)}</td>
                  <td className="px-3 py-3">{formatDate(item.holdExpiresAtUtc)}</td>
                </tr>
              ))}
              {!loading && applications.length === 0 && <tr><td colSpan={7} className="px-3 py-5 text-slate-500">No applications match these filters.</td></tr>}
            </tbody>
          </table>
        </div>
      </div>
      </div>
    </div>
  );
}

function DashboardLayout() {
  return (
    <BrowserRouter>
      <div className="min-h-screen bg-slate-100 text-slate-900">
        <header className="border-b border-slate-200 bg-white">
          <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-4 px-5 py-4">
            <div>
              <p className="text-xs uppercase tracking-[0.3em] text-sky-600">Admission</p>
              <Link to="/" className="text-xl font-bold text-slate-900">School Admission System</Link>
            </div>
            <nav className="flex flex-wrap gap-3 text-sm font-medium text-slate-600">
              <Link to="/" className="rounded-lg px-3 py-2 hover:bg-slate-100">Home</Link>
              <Link to="/classes" className="rounded-lg px-3 py-2 hover:bg-slate-100">Classes</Link>
              <Link to="/apply" className="rounded-lg px-3 py-2 hover:bg-slate-100">Apply</Link>
              <Link to="/applications" className="rounded-lg px-3 py-2 hover:bg-slate-100">My Applications</Link>
              <Link to="/admin" className="rounded-lg px-3 py-2 hover:bg-slate-100">Admin</Link>
            </nav>
          </div>
        </header>
        <main className="mx-auto max-w-7xl px-5 py-8">
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/classes" element={<ClassesPage />} />
            <Route path="/classes/:classId" element={<ClassDetailsPage />} />
            <Route path="/apply" element={<ApplicationFormPage />} />
            <Route path="/applications" element={<MyApplicationsPage />} />
            <Route path="/applications/:applicationId/status" element={<ApplicationStatusPage />} />
            <Route path="/payment/:applicationId" element={<PaymentPage />} />
            <Route path="/admin" element={<AdminDashboardPage />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}

function App() {
  return <DashboardLayout />;
}

export default App;
