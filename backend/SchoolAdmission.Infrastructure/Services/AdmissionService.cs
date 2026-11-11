using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;
using SchoolAdmission.Core.Interfaces;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.Infrastructure.Services;

public class AdmissionService : IAdmissionService
{
    private readonly SchoolAdmissionDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AdmissionService(SchoolAdmissionDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<ShiftDto>> GetShiftsAsync()
    {
        return await _dbContext.Shifts
            .OrderBy(s => s.Name)
            .Select(s => new ShiftDto(s.Id, s.Name))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<SchoolClassDto>> GetClassesAsync()
    {
        var classes = await _dbContext.SchoolClasses
            .Include(c => c.ClassShifts)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return classes.Select(c =>
        {
            var totalSeats = c.ClassShifts.Sum(cs => cs.TotalSeats);
            var heldSeats = c.ClassShifts.Sum(cs => cs.HeldSeats);
            var confirmedSeats = c.ClassShifts.Sum(cs => cs.ConfirmedSeats);
            return new SchoolClassDto(c.Id, c.Name, totalSeats, heldSeats, confirmedSeats, totalSeats - heldSeats - confirmedSeats, c.CreatedAtUtc);
        }).ToList();
    }

    public async Task<SchoolClassDto?> GetClassAsync(Guid classId)
    {
        var schoolClass = await _dbContext.SchoolClasses
            .Include(c => c.ClassShifts)
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (schoolClass is null)
        {
            return null;
        }

        var totalSeats = schoolClass.ClassShifts.Sum(cs => cs.TotalSeats);
        var heldSeats = schoolClass.ClassShifts.Sum(cs => cs.HeldSeats);
        var confirmedSeats = schoolClass.ClassShifts.Sum(cs => cs.ConfirmedSeats);

        return new SchoolClassDto(schoolClass.Id, schoolClass.Name, totalSeats, heldSeats, confirmedSeats, totalSeats - heldSeats - confirmedSeats, schoolClass.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<ClassAvailabilityDto>> GetClassAvailabilityAsync(Guid? classId = null)
    {
        var query = _dbContext.ClassShifts
            .Include(cs => cs.SchoolClass)
            .Include(cs => cs.Shift)
            .AsQueryable();

        if (classId.HasValue)
        {
            query = query.Where(cs => cs.SchoolClassId == classId.Value);
        }

        var items = await query
            .OrderBy(cs => cs.SchoolClass.Name)
            .ThenBy(cs => cs.Shift.Name)
            .Select(cs => new ClassAvailabilityDto(
                cs.Id,
                cs.SchoolClassId,
                cs.SchoolClass.Name,
                cs.ShiftId,
                cs.Shift.Name,
                cs.TotalSeats,
                cs.HeldSeats,
                cs.ConfirmedSeats,
                cs.TotalSeats - cs.HeldSeats - cs.ConfirmedSeats))
            .ToListAsync();

        return items;
    }

    public async Task<SchoolClassDto> CreateClassAsync(CreateClassRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Class name is required.");
        }

        if (request.TotalSeats <= 0)
        {
            throw new InvalidOperationException("Class total seats must be greater than zero.");
        }

        var schoolClass = new SchoolClass
        {
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.SchoolClasses.Add(schoolClass);
        await _dbContext.SaveChangesAsync();

        return new SchoolClassDto(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats, 0, 0, schoolClass.TotalSeats, schoolClass.CreatedAtUtc);
    }

    public async Task<SchoolClassDto> CreateClassWithShiftAsync(CreateClassWithShiftRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length < 2)
        {
            throw new InvalidOperationException("Class name must contain at least two characters.");
        }

        if (request.TotalSeats <= 0)
        {
            throw new InvalidOperationException("Class seat capacity must be greater than zero.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var shift = await _dbContext.Shifts.SingleOrDefaultAsync(item => item.Id == request.ShiftId);
        if (shift is null)
        {
            throw new KeyNotFoundException("Selected shift was not found.");
        }

        var schoolClass = new SchoolClass
        {
            Name = name,
            TotalSeats = request.TotalSeats,
            CreatedAtUtc = DateTime.UtcNow
        };
        var classShift = new ClassShift
        {
            SchoolClass = schoolClass,
            ShiftId = shift.Id,
            TotalSeats = request.TotalSeats,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ClassShifts.Add(classShift);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SchoolClassDto(
            schoolClass.Id,
            schoolClass.Name,
            classShift.TotalSeats,
            classShift.HeldSeats,
            classShift.ConfirmedSeats,
            classShift.AvailableSeats,
            schoolClass.CreatedAtUtc);
    }

    public async Task<SchoolClassDto> CreateClassShiftAsync(CreateClassShiftRequest request)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (!await LockSchoolClassAsync(request.ClassId))
        {
            throw new KeyNotFoundException("Class not found.");
        }

        var schoolClass = await _dbContext.SchoolClasses.FirstOrDefaultAsync(c => c.Id == request.ClassId);
        if (schoolClass is null)
        {
            throw new KeyNotFoundException("Class not found.");
        }

        var shift = await _dbContext.Shifts.FirstOrDefaultAsync(s => s.Id == request.ShiftId);
        if (shift is null)
        {
            throw new KeyNotFoundException("Shift not found.");
        }

        if (request.TotalSeats <= 0)
        {
            throw new InvalidOperationException("Seat capacity must be greater than zero.");
        }

        var existing = await _dbContext.ClassShifts.AnyAsync(cs => cs.SchoolClassId == request.ClassId && cs.ShiftId == request.ShiftId);
        if (existing)
        {
            throw new InvalidOperationException("A class + shift configuration already exists.");
        }

        var classShift = new ClassShift
        {
            SchoolClassId = request.ClassId,
            ShiftId = request.ShiftId,
            TotalSeats = request.TotalSeats,
            HeldSeats = 0,
            ConfirmedSeats = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ClassShifts.Add(classShift);
        await _dbContext.SaveChangesAsync();

        var classSeats = await _dbContext.ClassShifts
            .Where(item => item.SchoolClassId == schoolClass.Id)
            .SumAsync(item => item.TotalSeats);
        var heldSeats = await _dbContext.ClassShifts
            .Where(item => item.SchoolClassId == schoolClass.Id)
            .SumAsync(item => item.HeldSeats);
        var confirmedSeats = await _dbContext.ClassShifts
            .Where(item => item.SchoolClassId == schoolClass.Id)
            .SumAsync(item => item.ConfirmedSeats);
        schoolClass.TotalSeats = classSeats;
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new SchoolClassDto(
            schoolClass.Id,
            schoolClass.Name,
            classSeats,
            heldSeats,
            confirmedSeats,
            classSeats - heldSeats - confirmedSeats,
            schoolClass.CreatedAtUtc);
    }

    public async Task<ShiftDto> CreateShiftAsync(CreateShiftRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length < 2)
        {
            throw new InvalidOperationException("Shift name must contain at least two characters.");
        }

        if (await _dbContext.Shifts.AnyAsync(shift => shift.Name.ToLower() == name.ToLower()))
        {
            throw new InvalidOperationException("A shift with this name already exists.");
        }

        var shift = new Shift { Name = name, CreatedAtUtc = DateTime.UtcNow };
        _dbContext.Shifts.Add(shift);
        await _dbContext.SaveChangesAsync();
        return new ShiftDto(shift.Id, shift.Name);
    }

    public async Task<ClassAvailabilityDto> UpdateClassShiftCapacityAsync(Guid classShiftId, int totalSeats)
    {
        if (totalSeats <= 0)
        {
            throw new InvalidOperationException("Seat capacity must be greater than zero.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (!await LockClassShiftAsync(classShiftId))
        {
            throw new KeyNotFoundException("Class and shift configuration not found.");
        }

        var classShift = await _dbContext.ClassShifts
            .Include(item => item.SchoolClass)
            .Include(item => item.Shift)
            .SingleAsync(item => item.Id == classShiftId);

        if (!await LockSchoolClassAsync(classShift.SchoolClassId))
        {
            throw new InvalidOperationException("The class for this seat configuration no longer exists.");
        }

        await _dbContext.Entry(classShift.SchoolClass).ReloadAsync();
        if (totalSeats < classShift.HeldSeats + classShift.ConfirmedSeats)
        {
            throw new InvalidOperationException("Capacity cannot be lower than held and confirmed seats.");
        }

        classShift.TotalSeats = totalSeats;
        await _dbContext.SaveChangesAsync();
        classShift.SchoolClass.TotalSeats = await _dbContext.ClassShifts
            .Where(item => item.SchoolClassId == classShift.SchoolClassId)
            .SumAsync(item => item.TotalSeats);
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ClassAvailabilityDto(
            classShift.Id,
            classShift.SchoolClassId,
            classShift.SchoolClass.Name,
            classShift.ShiftId,
            classShift.Shift.Name,
            classShift.TotalSeats,
            classShift.HeldSeats,
            classShift.ConfirmedSeats,
            classShift.AvailableSeats);
    }

    public async Task<ApplicationSubmissionResponse> CreateApplicationAsync(CreateApplicationRequest request)
    {
        var normalizedName = request.StudentName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new InvalidOperationException("Student name is required.");
        }

        if (!IsValidBirthRegistrationNumber(request.BirthRegistrationNumber))
        {
            throw new InvalidOperationException("Birth registration number must contain exactly 17 digits.");
        }

        if (request.ClassShiftId == Guid.Empty)
        {
            throw new InvalidOperationException("Class shift is required.");
        }

        var holdDurationSeconds = _configuration.GetValue<int>("HOLD_DURATION_SECONDS", 600);
        if (holdDurationSeconds <= 0)
        {
            throw new InvalidOperationException("The configured hold duration must be greater than zero.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        await LockStudentRegistrationNumberAsync(request.BirthRegistrationNumber);
        if (!await LockClassShiftAsync(request.ClassShiftId))
        {
            throw new KeyNotFoundException("Selected class and shift were not found.");
        }

        var classShift = await _dbContext.ClassShifts
            .Include(cs => cs.SchoolClass)
            .Include(cs => cs.Shift)
            .FirstOrDefaultAsync(cs => cs.Id == request.ClassShiftId);

        if (classShift is null)
        {
            throw new KeyNotFoundException("Selected class and shift were not found.");
        }

        var now = DateTime.UtcNow;

        var activeHoldExists = await _dbContext.Applications
            .AnyAsync(a =>
                a.Student.BirthRegistrationNumber == request.BirthRegistrationNumber &&
                a.Status == ApplicationStatus.Pending &&
                a.HoldExpiresAtUtc > now);

        if (activeHoldExists)
        {
            throw new InvalidOperationException("A student cannot have more than one active seat hold at a time.");
        }

        if (classShift.AvailableSeats <= 0)
        {
            throw new InvalidOperationException("No seats are available for the selected class and shift.");
        }

        var student = await _dbContext.Students
            .SingleOrDefaultAsync(s => s.BirthRegistrationNumber == request.BirthRegistrationNumber);

        if (student is not null)
        {
            student.FullName = normalizedName;
        }
        else
        {
            student = new Student
            {
                FullName = normalizedName,
                BirthRegistrationNumber = request.BirthRegistrationNumber,
                CreatedAtUtc = now
            };

            _dbContext.Students.Add(student);
            await _dbContext.SaveChangesAsync();
        }

        var application = new AdmissionApplication
        {
            StudentId = student.Id,
            ClassShiftId = classShift.Id,
            Status = ApplicationStatus.Pending,
            CreatedAtUtc = now,
            HoldExpiresAtUtc = now.AddSeconds(holdDurationSeconds)
        };

        _dbContext.Applications.Add(application);
        classShift.HeldSeats += 1;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApplicationSubmissionResponse(
            application.Id,
            student.Id,
            classShift.Id,
            classShift.SchoolClass.Name,
            classShift.Shift.Name,
            application.Status.ToString(),
            application.HoldExpiresAtUtc);
    }
    public async Task<ApplicationDetailDto?> GetApplicationAsync(Guid applicationId)
    {
        var application = await _dbContext.Applications
            .Include(a => a.Student)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.SchoolClass)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.Shift)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            return null;
        }

        return new ApplicationDetailDto(
            application.Id,
            application.StudentId,
            application.Student.FullName,
            application.Student.BirthRegistrationNumber,
            application.ClassShiftId,
            application.ClassShift.SchoolClass.Name,
            application.ClassShift.Shift.Name,
            application.Status,
            application.CreatedAtUtc,
            application.HoldExpiresAtUtc,
            application.ConfirmedAtUtc,
            application.ExpiredAtUtc);
    }

    public async Task<IReadOnlyList<ApplicationDetailDto>> GetStudentApplicationsAsync(Guid studentId)
    {
        var applications = await _dbContext.Applications
            .Include(a => a.Student)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.SchoolClass)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.Shift)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();

        return applications.Select(a => new ApplicationDetailDto(
            a.Id,
            a.StudentId,
            a.Student.FullName,
            a.Student.BirthRegistrationNumber,
            a.ClassShiftId,
            a.ClassShift.SchoolClass.Name,
            a.ClassShift.Shift.Name,
            a.Status,
            a.CreatedAtUtc,
            a.HoldExpiresAtUtc,
            a.ConfirmedAtUtc,
            a.ExpiredAtUtc)).ToList();
    }

    public async Task<ApplicationStatusDto> GetApplicationStatusAsync(Guid applicationId)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        return new ApplicationStatusDto(application.Id, application.Status, application.CreatedAtUtc, application.HoldExpiresAtUtc, application.HoldExpiresAtUtc <= DateTime.UtcNow && application.Status == ApplicationStatus.Pending);
    }

    public async Task<PaymentConfirmationResponse> ConfirmPaymentAsync(Guid applicationId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);

        if (!await LockApplicationAsync(applicationId))
        {
            throw new KeyNotFoundException("Application not found.");
        }

        var application = await _dbContext.Applications
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.SchoolClass)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.Shift)
            .FirstOrDefaultAsync(a => a.Id == applicationId);

        if (application is null)
        {
            throw new KeyNotFoundException("Application not found.");
        }

        if (!await LockClassShiftAsync(application.ClassShiftId))
        {
            throw new InvalidOperationException("The application seat configuration no longer exists.");
        }

        var existingPaymentRecord = await _dbContext.Payments.FirstOrDefaultAsync(p => p.ApplicationId == applicationId);
        if (existingPaymentRecord is not null)
        {
            return new PaymentConfirmationResponse(existingPaymentRecord.Id, applicationId, existingPaymentRecord.Amount, existingPaymentRecord.Status.ToString(), existingPaymentRecord.ConfirmedAtUtc ?? existingPaymentRecord.CreatedAtUtc);
        }

        if (application.Status == ApplicationStatus.Confirmed)
        {
            throw new InvalidOperationException("Application is already confirmed.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending applications can be confirmed.");
        }

        if (application.HoldExpiresAtUtc <= DateTime.UtcNow)
        {
            application.Status = ApplicationStatus.Expired;
            application.ExpiredAtUtc = DateTime.UtcNow;
            ReleaseHeldSeat(application.ClassShift);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            throw new InvalidOperationException("Seat hold has expired. Payment cannot be confirmed.");
        }

        var payment = new Payment
        {
            ApplicationId = application.Id,
            Amount = 0m,
            Status = PaymentStatus.Paid,
            ConfirmedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Payments.Add(payment);
        application.Status = ApplicationStatus.Confirmed;
        application.ConfirmedAtUtc = payment.ConfirmedAtUtc;
        ReleaseHeldSeat(application.ClassShift);
        application.ClassShift.ConfirmedSeats += 1;

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return new PaymentConfirmationResponse(payment.Id, application.Id, payment.Amount, payment.Status.ToString(), payment.ConfirmedAtUtc ?? payment.CreatedAtUtc);
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var classes = await _dbContext.SchoolClasses.Include(c => c.ClassShifts).ToListAsync();
        var totalSeats = classes.Sum(c => c.TotalSeats) + classes.SelectMany(c => c.ClassShifts).Sum(cs => cs.TotalSeats) - classes.Sum(c => c.ClassShifts.Sum(cs => cs.TotalSeats));
        var totalClasses = classes.Count;
        var totalSeatsCurrent = classes.Sum(c => c.ClassShifts.Sum(cs => cs.TotalSeats));
        var heldSeats = classes.Sum(c => c.ClassShifts.Sum(cs => cs.HeldSeats));
        var confirmedAdmissions = classes.Sum(c => c.ClassShifts.Sum(cs => cs.ConfirmedSeats));
        var availableSeats = totalSeatsCurrent - heldSeats - confirmedAdmissions;
        var pendingApplications = await _dbContext.Applications.CountAsync(a => a.Status == ApplicationStatus.Pending);
        var expiredApplications = await _dbContext.Applications.CountAsync(a => a.Status == ApplicationStatus.Expired);

        return new DashboardStatsDto(totalClasses, totalSeatsCurrent, heldSeats, confirmedAdmissions, availableSeats, pendingApplications, expiredApplications);
    }

    public async Task<IReadOnlyList<AdmissionApplicationListItem>> GetAdminApplicationsAsync(Guid? classId = null, Guid? shiftId = null, ApplicationStatus? status = null, string? search = null)
    {
        var query = _dbContext.Applications
            .Include(a => a.Student)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.SchoolClass)
            .Include(a => a.ClassShift)
            .ThenInclude(cs => cs.Shift)
            .AsQueryable();

        if (classId.HasValue)
        {
            query = query.Where(a => a.ClassShift.SchoolClassId == classId.Value);
        }

        if (shiftId.HasValue)
        {
            query = query.Where(a => a.ClassShift.ShiftId == shiftId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(a => a.Student.FullName.Contains(search) || a.Student.BirthRegistrationNumber.Contains(search));
        }

        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AdmissionApplicationListItem(
                a.Id,
                a.StudentId,
                a.Student.FullName,
                a.Student.BirthRegistrationNumber,
                a.ClassShift.SchoolClassId,
                a.ClassShift.SchoolClass.Name,
                a.ClassShift.ShiftId,
                a.ClassShift.Shift.Name,
                a.Status,
                a.CreatedAtUtc,
                a.HoldExpiresAtUtc,
                a.ConfirmedAtUtc,
                a.ExpiredAtUtc))
            .ToListAsync();

        return items;
    }

    private static bool IsValidBirthRegistrationNumber(string value)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Length == 17 && value.All(character => character is >= '0' and <= '9');
    }

    private async Task LockStudentRegistrationNumberAsync(string registrationNumber)
    {
        await using var command = CreateCommand("SELECT pg_advisory_xact_lock(hashtextextended(@registrationNumber, 0))");
        AddParameter(command, "registrationNumber", registrationNumber);
        await command.ExecuteScalarAsync();
    }

    private Task<bool> LockClassShiftAsync(Guid classShiftId) =>
        LockRowAsync("SELECT \"Id\" FROM \"ClassShifts\" WHERE \"Id\" = @id FOR UPDATE", classShiftId);

    private Task<bool> LockApplicationAsync(Guid applicationId) =>
        LockRowAsync("SELECT \"Id\" FROM \"Applications\" WHERE \"Id\" = @id FOR UPDATE", applicationId);

    private Task<bool> LockSchoolClassAsync(Guid classId) =>
        LockRowAsync("SELECT \"Id\" FROM \"SchoolClasses\" WHERE \"Id\" = @id FOR UPDATE", classId);

    private async Task<bool> LockRowAsync(string sql, Guid id)
    {
        await using var command = CreateCommand(sql);
        AddParameter(command, "id", id);
        return await command.ExecuteScalarAsync() is not null;
    }

    private DbCommand CreateCommand(string sql)
    {
        var command = _dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void ReleaseHeldSeat(ClassShift classShift)
    {
        if (classShift.HeldSeats <= 0)
        {
            throw new InvalidOperationException("The application seat hold is inconsistent and cannot be released.");
        }

        classShift.HeldSeats -= 1;
    }
}
