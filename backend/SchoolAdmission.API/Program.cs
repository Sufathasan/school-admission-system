using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using SchoolAdmission.API.Extensions;
using SchoolAdmission.API.Middleware;
using SchoolAdmission.Infrastructure.Data;

namespace SchoolAdmission.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection to a PostgreSQL connection string before starting the API.");
        }

        builder.Services.AddDbContext<SchoolAdmissionDbContext>(options => options.UseNpgsql(connectionString));

        builder.Services.AddSchoolAdmissionServices();

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options => options.SerializeAsV2 = true);
            app.UseSwaggerUI();
        }

        app.UseCors("DefaultPolicy");
        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();

        await DatabaseInitializer.InitializeAsync(app);
        app.Run();
    }
}
