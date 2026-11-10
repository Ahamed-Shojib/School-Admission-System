using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Application.Abstractions;

public sealed record HoldView(Hold Hold, string ClassName, string? ClassShift);

public interface IHoldRepository
{
    Task AddAsync(Hold hold, CancellationToken ct);

    Task<Hold?> GetAsync(string holdId, CancellationToken ct);

    /// <summary>Reads only the class id, without tracking the hold, so the hold can be loaded fresh after the class lock is taken.</summary>
    Task<string?> GetClassIdAsync(string holdId, CancellationToken ct);

    /// <summary>Takes a transaction-scoped lock for this student (released at commit or rollback). Always taken before the class lock,
    /// so one student's apply/confirm requests run one at a time even when they touch different classes.</summary>
    Task LockStudentAsync(string studentId, CancellationToken ct);

    /// <summary>The student's CONFIRMED hold in any class, with the class name, if there is one.</summary>
    Task<HoldView?> FindConfirmedAsync(string studentId, CancellationToken ct);

    /// <summary>The student's HELD or CONFIRMED hold in this class, if any.</summary>
    Task<Hold?> FindActiveAsync(string classId, string studentId, CancellationToken ct);

    /// <summary>Number of HELD plus CONFIRMED holds. Call ExpireStaleAsync first so expired holds are not counted.</summary>
    Task<int> CountActiveAsync(string classId, CancellationToken ct);

    /// <summary>Marks overdue HELD holds as EXPIRED. Returns how many changed.</summary>
    Task<int> ExpireStaleAsync(string classId, DateTime now, CancellationToken ct);

    Task<IReadOnlyList<string>> GetClassIdsWithStaleHoldsAsync(DateTime now, CancellationToken ct);

    Task<IReadOnlyList<HoldView>> ListForStudentAsync(string studentId, CancellationToken ct);
}
