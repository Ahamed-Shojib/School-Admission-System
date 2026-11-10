using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Repositories;

public sealed class HoldRepository : IHoldRepository
{
    private readonly AppDbContext _db;

    public HoldRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(Hold hold, CancellationToken ct) => await _db.Holds.AddAsync(hold, ct);

    public Task<Hold?> GetAsync(string holdId, CancellationToken ct) =>
        _db.Holds.FirstOrDefaultAsync(h => h.Id == holdId, ct);

    public Task<string?> GetClassIdAsync(string holdId, CancellationToken ct) =>
        _db.Holds.AsNoTracking().Where(h => h.Id == holdId).Select(h => (string?)h.ClassId).FirstOrDefaultAsync(ct);

    public async Task LockStudentAsync(string studentId, CancellationToken ct) =>
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({studentId}))", ct);

    public async Task<HoldView?> FindConfirmedAsync(string studentId, CancellationToken ct)
    {
        var row = await _db.Holds.AsNoTracking()
            .Where(h => h.StudentId == studentId && h.Status == HoldStatus.Confirmed)
            .Join(_db.Classes, h => h.ClassId, c => c.Id, (h, c) => new { Hold = h, ClassName = c.Name, ClassShift = c.Shift })
            .FirstOrDefaultAsync(ct);
        return row is null ? null : new HoldView(row.Hold, row.ClassName, row.ClassShift);
    }

    public Task<Hold?> FindActiveAsync(string classId, string studentId, CancellationToken ct) =>
        _db.Holds.FirstOrDefaultAsync(h =>
            h.ClassId == classId &&
            h.StudentId == studentId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);

    public Task<int> CountActiveAsync(string classId, CancellationToken ct) =>
        _db.Holds.CountAsync(h =>
            h.ClassId == classId &&
            (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);

    public Task<int> ExpireStaleAsync(string classId, DateTime now, CancellationToken ct) =>
        _db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Status, HoldStatus.Expired), ct);

    public async Task<IReadOnlyList<string>> GetClassIdsWithStaleHoldsAsync(DateTime now, CancellationToken ct) =>
        await _db.Holds.AsNoTracking()
            .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .Select(h => h.ClassId)
            .Distinct()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HoldView>> ListForStudentAsync(string studentId, CancellationToken ct)
    {
        var rows = await _db.Holds.AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .Join(_db.Classes, h => h.ClassId, c => c.Id, (h, c) => new { Hold = h, ClassName = c.Name, ClassShift = c.Shift })
            .OrderByDescending(r => r.Hold.CreatedAt)
            .ToListAsync(ct);
        return rows.Select(r => new HoldView(r.Hold, r.ClassName, r.ClassShift)).ToList();
    }
}
