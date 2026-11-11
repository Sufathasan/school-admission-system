using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Core.Entities;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.API.Extensions;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SchoolAdmissionDbContext>();

        await context.Database.MigrateAsync();

        if (!await context.Shifts.AnyAsync())
        {
            var shifts = new[]
            {
                new Shift { Name = "Morning" },
                new Shift { Name = "Day" },
                new Shift { Name = "Evening" }
            };

            context.Shifts.AddRange(shifts);
            await context.SaveChangesAsync();
        }

        if (!await context.SchoolClasses.AnyAsync())
        {
            var shifts = await context.Shifts.ToListAsync();
            var classNames = new[] { "Science", "Business Studies", "Arts" };

            foreach (var className in classNames)
            {
                var schoolClass = new SchoolClass
                {
                    Name = className,
                    TotalSeats = shifts.Count * 30,
                    CreatedAtUtc = DateTime.UtcNow
                };

                context.SchoolClasses.Add(schoolClass);
                await context.SaveChangesAsync();

                foreach (var shift in shifts)
                {
                    context.ClassShifts.Add(new ClassShift
                    {
                        SchoolClassId = schoolClass.Id,
                        ShiftId = shift.Id,
                        TotalSeats = 30,
                        HeldSeats = 0,
                        ConfirmedSeats = 0,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
