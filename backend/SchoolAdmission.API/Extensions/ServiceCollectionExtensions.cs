using SchoolAdmission.Core.Interfaces;
using SchoolAdmission.Infrastructure.Data;
using SchoolAdmission.Infrastructure.Services;

namespace SchoolAdmission.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchoolAdmissionServices(this IServiceCollection services)
    {
        services.AddScoped<IAdmissionService, AdmissionService>();
        services.AddHostedService<ExpirationBackgroundService>();
        return services;
    }
}
