using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Auth;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Application.Options;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Infrastructure;

namespace SchoolAdmission.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "Frontend";

    public static IServiceCollection AddAdmissionApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        // Invalid or missing request bodies use the same { code, message } error shape as every other error.
        services.Configure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = _ =>
                new BadRequestObjectResult(new ErrorResponse("VALIDATION_ERROR", "The request body is missing or invalid.")));

        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

        var holdSeconds = configuration.GetValue<int?>("HOLD_DURATION_SECONDS") ?? HoldOptions.DefaultDurationSeconds;
        if (holdSeconds < 1)
            throw new InvalidOperationException("HOLD_DURATION_SECONDS must be a positive number of seconds.");
        services.AddSingleton(new HoldOptions { DurationSeconds = holdSeconds });

        var connectionString = configuration.GetConnectionString("Default")
                               ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");
        services.AddInfrastructure(connectionString);

        var tokenSecret = configuration["ADMIN_TOKEN_SECRET"] ?? "dev-only-secret-change-me-before-real-use";
        if (tokenSecret.Length < 16)
            throw new InvalidOperationException("ADMIN_TOKEN_SECRET must be at least 16 characters.");
        services.AddSingleton(new AdminAuthOptions
        {
            Username = configuration["ADMIN_USERNAME"] ?? "admin",
            Password = configuration["ADMIN_PASSWORD"] ?? "admin123",
            TokenSecret = tokenSecret,
            TokenLifetime = TimeSpan.FromHours(configuration.GetValue<double?>("ADMIN_TOKEN_HOURS") ?? 8),
            Required = configuration.GetValue<bool?>("ADMIN_AUTH_REQUIRED") ?? true
        });
        services.AddSingleton<AdminTokenService>();

        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IHoldService, HoldService>();
        return services;
    }
}
