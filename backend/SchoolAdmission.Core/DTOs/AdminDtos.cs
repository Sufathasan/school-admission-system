using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Core.DTOs;

public record DashboardStatsDto(int TotalClasses, int TotalSeats, int HeldSeats, int ConfirmedAdmissions, int AvailableSeats, int PendingApplications, int ExpiredApplications);

public record AdmissionApplicationListItem(Guid Id, Guid StudentId, string StudentName, string BirthRegistrationNumber, Guid ClassId, string ClassName, Guid ShiftId, string ShiftName, ApplicationStatus Status, DateTime CreatedAtUtc, DateTime HoldExpiresAtUtc, DateTime? ConfirmedAtUtc, DateTime? ExpiredAtUtc);
