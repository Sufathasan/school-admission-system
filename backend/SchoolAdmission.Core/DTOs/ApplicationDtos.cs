using System.ComponentModel.DataAnnotations;
using SchoolAdmission.Core.Enums;

namespace SchoolAdmission.Core.DTOs;

public record CreateApplicationRequest(
    [property: Required, StringLength(200, MinimumLength = 2)] string StudentName,
    [property: Required, RegularExpression("^\\d{17}$", ErrorMessage = "Birth registration number must contain exactly 17 digits.")] string BirthRegistrationNumber,
    Guid ClassShiftId);

public record ApplicationSubmissionResponse(Guid ApplicationId, Guid StudentId, Guid ClassShiftId, string ClassName, string ShiftName, string Status, DateTime HoldExpiresAtUtc);

public record ApplicationDetailDto(Guid Id, Guid StudentId, string StudentName, string BirthRegistrationNumber, Guid ClassShiftId, string ClassName, string ShiftName, ApplicationStatus Status, DateTime CreatedAtUtc, DateTime HoldExpiresAtUtc, DateTime? ConfirmedAtUtc, DateTime? ExpiredAtUtc);

public record ApplicationStatusDto(Guid ApplicationId, ApplicationStatus Status, DateTime CreatedAtUtc, DateTime HoldExpiresAtUtc, bool IsExpired);

public record PaymentConfirmationRequest(Guid ApplicationId);

public record PaymentConfirmationResponse(Guid PaymentId, Guid ApplicationId, decimal Amount, string Status, DateTime ConfirmedAtUtc);
