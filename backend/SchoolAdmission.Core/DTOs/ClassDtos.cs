using System.ComponentModel.DataAnnotations;

namespace SchoolAdmission.Core.DTOs;

public record CreateClassRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    [property: Range(1, int.MaxValue)] int TotalSeats);

public record CreateClassWithShiftRequest(
    [property: Required, StringLength(120, MinimumLength = 2)] string Name,
    Guid ShiftId,
    [property: Range(1, int.MaxValue)] int TotalSeats);

public record SchoolClassDto(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats, DateTime CreatedAtUtc);

public record ClassAvailabilityDto(Guid ClassShiftId, Guid ClassId, string ClassName, Guid ShiftId, string ShiftName, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats);

public record CreateClassShiftRequest(Guid ClassId, Guid ShiftId, int TotalSeats);

public record CreateShiftRequest([property: Required, StringLength(60, MinimumLength = 2)] string Name);

public record UpdateClassShiftCapacityRequest([property: Range(1, int.MaxValue)] int TotalSeats);

public record ShiftDto(Guid Id, string Name);
