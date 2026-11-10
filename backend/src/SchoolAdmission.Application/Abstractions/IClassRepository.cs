using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Application.Abstractions;

public sealed record ClassWithCounts(SchoolClass Class, int Held, int Confirmed);

public interface IClassRepository
{
    Task AddAsync(SchoolClass schoolClass, CancellationToken ct);

    /// <summary>Takes a row lock (SELECT ... FOR UPDATE) on the class for the rest of the transaction.
    /// All seat changes for one class are serialised through this lock, across any number of backend processes.</summary>
    Task<SchoolClass?> LockAsync(string classId, CancellationToken ct);

    Task<ClassWithCounts?> GetWithCountsAsync(string classId, DateTime now, CancellationToken ct);

    Task<IReadOnlyList<ClassWithCounts>> ListWithCountsAsync(DateTime now, CancellationToken ct);
}
