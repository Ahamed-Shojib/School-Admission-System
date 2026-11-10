using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Infrastructure.BackgroundJobs;

/// <summary>
/// Housekeeping: persists EXPIRED for overdue holds. Correctness does not depend on it, because every read
/// and every seat count already treats an overdue HELD hold as expired. It works class by class under the
/// same class lock as the API so it can never race with a booking.
/// </summary>
public sealed class HoldExpiryWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopes;
    private readonly IClock _clock;
    private readonly ILogger<HoldExpiryWorker> _logger;

    public HoldExpiryWorker(IServiceScopeFactory scopes, IClock clock, ILogger<HoldExpiryWorker> logger)
    {
        _scopes = scopes;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await ExpireOnceAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Hold expiry pass failed; will retry on the next tick.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task ExpireOnceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var holds = scope.ServiceProvider.GetRequiredService<IHoldRepository>();

        var classIds = await holds.GetClassIdsWithStaleHoldsAsync(_clock.UtcNow, ct);
        foreach (var classId in classIds)
        {
            using var classScope = _scopes.CreateScope();
            var sp = classScope.ServiceProvider;
            var classes = sp.GetRequiredService<IClassRepository>();
            var classHolds = sp.GetRequiredService<IHoldRepository>();
            var tx = sp.GetRequiredService<ITransactionRunner>();

            var changed = await tx.RunAsync(async () =>
            {
                await classes.LockAsync(classId, ct);
                return await classHolds.ExpireStaleAsync(classId, _clock.UtcNow, ct);
            }, ct);

            if (changed > 0)
                _logger.LogInformation("Expired {Count} hold(s) in class {ClassId}.", changed, classId);
        }
    }
}
