using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public sealed class ClassService : IClassService
{
    private readonly IClassRepository _classes;
    private readonly ITransactionRunner _tx;
    private readonly IClock _clock;

    public ClassService(IClassRepository classes, ITransactionRunner tx, IClock clock)
    {
        _classes = classes;
        _tx = tx;
        _clock = clock;
    }

    public Task<CreatedClassResponse> CreateAsync(CreateClassRequest request, CancellationToken ct) =>
        _tx.RunAsync(async () =>
        {
            var created = SchoolClass.Create(request.Name, request.Shift, request.TotalSeats, _clock.UtcNow);
            await _classes.AddAsync(created, ct);
            return new CreatedClassResponse(created.Id, created.Name, created.TotalSeats, created.Shift);
        }, ct);

    public async Task<IReadOnlyList<ClassResponse>> ListAsync(CancellationToken ct)
    {
        var rows = await _classes.ListWithCountsAsync(_clock.UtcNow, ct);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<ClassResponse> GetAsync(string classId, CancellationToken ct)
    {
        var row = await _classes.GetWithCountsAsync(classId, _clock.UtcNow, ct)
                  ?? throw new NotFoundException("CLASS_NOT_FOUND", "Class not found.");
        return ToResponse(row);
    }

    private static ClassResponse ToResponse(ClassWithCounts row) => new(
        row.Class.Id,
        row.Class.Name,
        row.Class.TotalSeats,
        row.Held,
        row.Confirmed,
        row.Class.TotalSeats - row.Held - row.Confirmed,
        row.Class.Shift);
}
