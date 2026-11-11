using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Core.Interfaces;

public interface IAdmissionService
{
    Task<IReadOnlyList<ShiftDto>> GetShiftsAsync();
    Task<IReadOnlyList<SchoolClassDto>> GetClassesAsync();
    Task<SchoolClassDto?> GetClassAsync(Guid classId);
    Task<IReadOnlyList<ClassAvailabilityDto>> GetClassAvailabilityAsync(Guid? classId = null);
    Task<SchoolClassDto> CreateClassAsync(CreateClassRequest request);
    Task<SchoolClassDto> CreateClassWithShiftAsync(CreateClassWithShiftRequest request);
    Task<SchoolClassDto> CreateClassShiftAsync(CreateClassShiftRequest request);
    Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request);
    Task<ClassAvailabilityDto> UpdateClassShiftCapacityAsync(Guid classShiftId, int totalSeats);
    Task<ApplicationSubmissionResponse> CreateApplicationAsync(CreateApplicationRequest request);
    Task<ApplicationDetailDto?> GetApplicationAsync(Guid applicationId);
    Task<IReadOnlyList<ApplicationDetailDto>> GetStudentApplicationsAsync(Guid studentId);
    Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid applicationId);
    Task<PaymentConfirmationResponse> ConfirmPaymentAsync(Guid applicationId);
    Task<DashboardStatsDto> GetDashboardStatsAsync();
    Task<IReadOnlyList<AdmissionApplicationListItem>> GetAdminApplicationsAsync(Guid? classId = null, Guid? shiftId = null, ApplicationStatus? status = null, string? search = null);
}
