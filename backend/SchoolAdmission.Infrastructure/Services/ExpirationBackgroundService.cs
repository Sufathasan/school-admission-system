using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Core.Enums;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.Infrastructure.Services;

public class ExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirationBackgroundService> _logger;
    private readonly TimeSpan _pollingInterval;

    public ExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExpirationBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _pollingInterval = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("EXPIRATION_POLLING_SECONDS", 15)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollingInterval);
        do
        {
            try
            {
                await ExpireDueApplicationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing expired admissions.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExpireDueApplicationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchoolAdmissionDbContext>();
        var applicationIds = await dbContext.Applications
            .AsNoTracking()
            .Where(application => application.Status == ApplicationStatus.Pending &&
                                  application.HoldExpiresAtUtc <= DateTime.UtcNow)
            .Select(application => application.Id)
            .ToListAsync(cancellationToken);

        foreach (var applicationId in applicationIds)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            if (!await LockRowAsync(dbContext, "Applications", applicationId, cancellationToken))
            {
                continue;
            }

            var application = await dbContext.Applications
                .Include(item => item.ClassShift)
                .FirstOrDefaultAsync(item => item.Id == applicationId, cancellationToken);

            if (application is null ||
                application.Status != ApplicationStatus.Pending ||
                application.HoldExpiresAtUtc > DateTime.UtcNow)
            {
                continue;
            }

            if (!await LockRowAsync(dbContext, "ClassShifts", application.ClassShiftId, cancellationToken))
            {
                throw new InvalidOperationException($"Class shift for application {applicationId} was not found.");
            }

            await dbContext.Entry(application.ClassShift).ReloadAsync(cancellationToken);
            if (application.ClassShift.HeldSeats <= 0)
            {
                throw new InvalidOperationException($"Application {applicationId} has no held seat to release.");
            }

            application.Status = ApplicationStatus.Expired;
            application.ExpiredAtUtc = DateTime.UtcNow;
            application.ClassShift.HeldSeats -= 1;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Expired application {ApplicationId} and released hold for class shift {ClassShiftId}",
                application.Id,
                application.ClassShiftId);

            dbContext.ChangeTracker.Clear();
        }
    }

    private static async Task<bool> LockRowAsync(
        SchoolAdmissionDbContext dbContext,
        string tableName,
        Guid id,
        CancellationToken cancellationToken)
    {
        var table = tableName switch
        {
            "Applications" => "\"Applications\"",
            "ClassShifts" => "\"ClassShifts\"",
            _ => throw new ArgumentOutOfRangeException(nameof(tableName))
        };

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = $"SELECT \"Id\" FROM {table} WHERE \"Id\" = @id FOR UPDATE";
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();

        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }
}
