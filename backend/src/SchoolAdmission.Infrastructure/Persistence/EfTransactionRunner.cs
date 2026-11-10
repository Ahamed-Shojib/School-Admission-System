using System.Data;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Infrastructure.Persistence;

public sealed class EfTransactionRunner : ITransactionRunner
{
    private readonly AppDbContext _db;

    public EfTransactionRunner(AppDbContext db) => _db = db;

    public async Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken ct)
    {
        // If work throws, the transaction is disposed without commit and rolls back.
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        var result = await work();
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }
}
