using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Repositories;

public sealed class ClassRepository : IClassRepository
{
    private readonly AppDbContext _db;

    public ClassRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(SchoolClass schoolClass, CancellationToken ct) =>
        await _db.Classes.AddAsync(schoolClass, ct);

    public async Task<SchoolClass?> LockAsync(string classId, CancellationToken ct)
    {
        // Must run inside a transaction: the row lock is held until commit or rollback.
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT 1 FROM classes WHERE \"Id\" = {classId} FOR UPDATE", ct);
        return await _db.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct);
    }

    public async Task<ClassWithCounts?> GetWithCountsAsync(string classId, DateTime now, CancellationToken ct)
    {
        var row = await CountedQuery(now, classId).FirstOrDefaultAsync(ct);
        return row is null ? null : new ClassWithCounts(row.Class, row.Held, row.Confirmed);
    }

    public async Task<IReadOnlyList<ClassWithCounts>> ListWithCountsAsync(DateTime now, CancellationToken ct)
    {
        var rows = await CountedQuery(now, null).OrderBy(r => r.Class.CreatedAt).ThenBy(r => r.Class.Id).ToListAsync(ct);
        return rows.Select(r => new ClassWithCounts(r.Class, r.Held, r.Confirmed)).ToList();
    }

    // A HELD hold only occupies a seat until its deadline, so counts stay right even before the expiry job runs.
    private IQueryable<CountedRow> CountedQuery(DateTime now, string? classId)
    {
        var classes = _db.Classes.AsNoTracking();
        if (classId is not null)
            classes = classes.Where(c => c.Id == classId);

        return classes.Select(c => new CountedRow
        {
            Class = c,
            Held = _db.Holds.Count(h => h.ClassId == c.Id && h.Status == HoldStatus.Held && h.ExpiresAt > now),
            Confirmed = _db.Holds.Count(h => h.ClassId == c.Id && h.Status == HoldStatus.Confirmed)
        });
    }

    private sealed class CountedRow
    {
        public SchoolClass Class { get; set; } = default!;
        public int Held { get; set; }
        public int Confirmed { get; set; }
    }
}
