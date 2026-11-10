namespace SchoolAdmission.Application.Abstractions;

/// <summary>Runs work in one database transaction; saves pending changes and commits, or rolls back if the work throws.</summary>
public interface ITransactionRunner
{
    Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken ct);
}
