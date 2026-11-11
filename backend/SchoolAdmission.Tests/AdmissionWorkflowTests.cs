using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SchoolAdmission.Core.DTOs;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Core.Enums;
using SchoolAdmission.Infrastructure.Data;
using SchoolAdmission.Infrastructure.Services;
using Testcontainers.PostgreSql;

namespace SchoolAdmission.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .Build();

    public Task InitializeAsync() => Container.StartAsync();

    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

public sealed class AdmissionWorkflowTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;
    private DbContextOptions<SchoolAdmissionDbContext> _dbOptions = null!;
    private Guid _classShiftId;
    private string _databaseName = string.Empty;
    private string _connectionString = string.Empty;

    public AdmissionWorkflowTests(PostgreSqlFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _databaseName = $"admission_{Guid.NewGuid():N}";
        await using (var adminConnection = new NpgsqlConnection(_fixture.Container.GetConnectionString()))
        {
            await adminConnection.OpenAsync();
            await using var createDatabase = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", adminConnection);
            await createDatabase.ExecuteNonQueryAsync();
        }

        _connectionString = new NpgsqlConnectionStringBuilder(_fixture.Container.GetConnectionString())
        {
            Database = _databaseName
        }.ConnectionString;
        _dbOptions = new DbContextOptionsBuilder<SchoolAdmissionDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        await using var dbContext = CreateDbContext();
        await dbContext.Database.EnsureCreatedAsync();
        var schoolClass = new SchoolClass { Name = "Grade 1", TotalSeats = 1 };
        var shift = new Shift { Name = "Morning" };
        var classShift = new ClassShift
        {
            SchoolClass = schoolClass,
            Shift = shift,
            TotalSeats = 1
        };
        dbContext.ClassShifts.Add(classShift);
        await dbContext.SaveChangesAsync();
        _classShiftId = classShift.Id;
    }

    public async Task DisposeAsync()
    {
        await using var adminConnection = new NpgsqlConnection(_fixture.Container.GetConnectionString());
        await adminConnection.OpenAsync();
        await using var dropDatabase = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", adminConnection);
        await dropDatabase.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Valid_application_creates_a_pending_hold()
    {
        var result = await ExecuteAsync(service => service.CreateApplicationAsync(Request("10000000000000001")));

        Assert.Equal("Pending", result.Status);
        Assert.True(result.HoldExpiresAtUtc > DateTime.UtcNow);

        await using var dbContext = CreateDbContext();
        var classShift = await dbContext.ClassShifts.SingleAsync();
        Assert.Equal(1, classShift.HeldSeats);
        Assert.Equal(0, classShift.AvailableSeats);
    }

    [Fact]
    public async Task Payment_confirmation_is_idempotent()
    {
        var application = await ExecuteAsync(service => service.CreateApplicationAsync(Request("10000000000000002")));

        var firstPayment = await ExecuteAsync(service => service.ConfirmPaymentAsync(application.ApplicationId));
        var repeatedPayment = await ExecuteAsync(service => service.ConfirmPaymentAsync(application.ApplicationId));

        Assert.Equal(firstPayment.PaymentId, repeatedPayment.PaymentId);
        await using var dbContext = CreateDbContext();
        Assert.Equal(1, await dbContext.Payments.CountAsync());
        var classShift = await dbContext.ClassShifts.SingleAsync();
        Assert.Equal(0, classShift.HeldSeats);
        Assert.Equal(1, classShift.ConfirmedSeats);
    }

    [Fact]
    public async Task Concurrent_applications_never_reserve_more_than_capacity()
    {
        var requests = Enumerable.Range(10, 12)
            .Select(number => Task.Run(async () =>
            {
                try
                {
                    await ExecuteAsync(service => service.CreateApplicationAsync(Request($"100000000000000{number:D2}")));
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(requests);

        Assert.Single(results.Where(success => success));
        await using var dbContext = CreateDbContext();
        var classShift = await dbContext.ClassShifts.SingleAsync();
        Assert.InRange(classShift.HeldSeats + classShift.ConfirmedSeats, 0, classShift.TotalSeats);
        Assert.Equal(1, await dbContext.Applications.CountAsync());
    }

    [Fact]
    public async Task Invalid_birth_registration_number_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExecuteAsync(service => service.CreateApplicationAsync(Request("12345"))));
    }

    [Fact]
    public async Task Student_cannot_create_a_second_active_hold()
    {
        var registrationNumber = "10000000000000003";
        await ExecuteAsync(service => service.CreateApplicationAsync(Request(registrationNumber)));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExecuteAsync(service => service.CreateApplicationAsync(Request(registrationNumber))));
    }

    [Fact]
    public async Task Expired_application_cannot_be_paid_and_its_hold_is_released()
    {
        var application = await ExecuteAsync(service => service.CreateApplicationAsync(Request("10000000000000004")));
        await using (var dbContext = CreateDbContext())
        {
            var storedApplication = await dbContext.Applications.SingleAsync();
            storedApplication.HoldExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await dbContext.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExecuteAsync(service => service.ConfirmPaymentAsync(application.ApplicationId)));

        await using var verificationContext = CreateDbContext();
        Assert.Equal(ApplicationStatus.Expired, (await verificationContext.Applications.SingleAsync()).Status);
        Assert.Equal(0, (await verificationContext.ClassShifts.SingleAsync()).HeldSeats);
        Assert.Empty(await verificationContext.Payments.ToListAsync());
    }

    [Fact]
    public async Task Capacity_cannot_be_reduced_below_occupied_seats()
    {
        await using (var setupContext = CreateDbContext())
        {
            var classShift = await setupContext.ClassShifts.SingleAsync();
            classShift.TotalSeats = 2;
            await setupContext.SaveChangesAsync();
        }

        await ExecuteAsync(service => service.CreateApplicationAsync(Request("10000000000000005")));
        await ExecuteAsync(service => service.CreateApplicationAsync(Request("10000000000000006")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ExecuteAsync(service => service.UpdateClassShiftCapacityAsync(_classShiftId, 1)));
    }

    [Fact]
    public async Task Background_worker_expires_holds_and_releases_capacity()
    {
        var application = await ExecuteAsync(service => service.CreateApplicationAsync(Request("10000000000000007")));
        await using (var dbContext = CreateDbContext())
        {
            var storedApplication = await dbContext.Applications.SingleAsync();
            storedApplication.HoldExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await dbContext.SaveChangesAsync();
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EXPIRATION_POLLING_SECONDS"] = "1" })
            .Build();
        using var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddDbContext<SchoolAdmissionDbContext>(options => options.UseNpgsql(_connectionString))
            .AddSingleton<ExpirationBackgroundService>()
            .BuildServiceProvider();
        var expirationWorker = services.GetRequiredService<ExpirationBackgroundService>();

        await expirationWorker.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await expirationWorker.StopAsync(CancellationToken.None);

        await using var verificationContext = CreateDbContext();
        Assert.Equal(ApplicationStatus.Expired, (await verificationContext.Applications.SingleAsync()).Status);
        Assert.Equal(0, (await verificationContext.ClassShifts.SingleAsync()).HeldSeats);
        Assert.NotEqual(Guid.Empty, application.ApplicationId);
    }

    private SchoolAdmissionDbContext CreateDbContext() => new(_dbOptions);

    private async Task<T> ExecuteAsync<T>(Func<AdmissionService, Task<T>> action)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["HOLD_DURATION_SECONDS"] = "600" })
            .Build();
        await using var dbContext = CreateDbContext();
        return await action(new AdmissionService(dbContext, configuration));
    }

    private CreateApplicationRequest Request(string registrationNumber) =>
        new("Test Student", registrationNumber, _classShiftId);
}
