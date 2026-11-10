using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseAdmissionPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors(ServiceCollectionExtensions.CorsPolicy);
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        return app;
    }

    /// <summary>
    /// "One confirmed class per student" is enforced by the service under a per-student lock; this index is the database-level
    /// backstop. Added with IF NOT EXISTS for databases created before it existed. If old data already breaks the rule the index
    /// cannot be built: that is logged and the app still starts, since the service-level check does not depend on the index.
    /// </summary>
    private static async Task EnsureConfirmedStudentIndexAsync(AppDbContext db, ILogger logger)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_holds_StudentId_confirmed\" ON holds (\"StudentId\") WHERE \"Status\" = 'CONFIRMED'");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not create the one-confirmed-class-per-student index (existing data may already break the rule): {Message}", ex.Message);
        }
    }

    /// <summary>Creates the schema on first start. Retries because the database container may still be starting.</summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        const int maxAttempts = 30;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.EnsureCreatedAsync();
                // EnsureCreated never alters an existing table, so a database made before the Shift field existed
                // gets the column added here. Safe to run every start.
                await db.Database.ExecuteSqlRawAsync("ALTER TABLE classes ADD COLUMN IF NOT EXISTS \"Shift\" character varying(32) NULL");
                await EnsureConfirmedStudentIndexAsync(db, app.Logger);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                app.Logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }
    }
}
