using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Infrastructure.BackgroundJobs;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Repositories;

namespace SchoolAdmission.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddScoped<IClassRepository, ClassRepository>();
        services.AddScoped<IHoldRepository, HoldRepository>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddHostedService<HoldExpiryWorker>();
        return services;
    }
}
