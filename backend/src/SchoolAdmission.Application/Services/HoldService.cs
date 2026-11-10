using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Options;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public sealed class HoldService : IHoldService
{
    private readonly IClassRepository _classes;
    private readonly IHoldRepository _holds;
    private readonly ITransactionRunner _tx;
    private readonly IClock _clock;
    private readonly HoldOptions _options;

    public HoldService(
        IClassRepository classes,
        IHoldRepository holds,
        ITransactionRunner tx,
        IClock clock,
        HoldOptions options)
    {
        _classes = classes;
        _holds = holds;
        _tx = tx;
        _clock = clock;
        _options = options;
    }

    /// <summary>
    /// Seat safety: the class row is locked first, so concurrent applications for one class run one at a time
    /// (also across backend processes sharing the database). Then expired holds are released, the student's
    /// existing application is checked, seats are counted and the hold inserted. Held + confirmed never exceeds totalSeats.
    /// </summary>
    public Task<HoldResponse> CreateAsync(string classId, string studentId, CancellationToken ct) =>
        _tx.RunAsync(async () =>
        {
            var now = _clock.UtcNow;

            // Lock order is always: student, then class. This keeps one student's requests for different classes in line.
            await _holds.LockStudentAsync(studentId, ct);

            var schoolClass = await _classes.LockAsync(classId, ct)
                              ?? throw new NotFoundException("CLASS_NOT_FOUND", "Class not found.");

            await _holds.ExpireStaleAsync(classId, now, ct);

            await EnsureNotConfirmedAsync(studentId, ct);

            if (await _holds.FindActiveAsync(classId, studentId, ct) is not null)
                throw new ConflictException("ALREADY_APPLIED", "This student already has an active application in this class.");

            if (await _holds.CountActiveAsync(classId, ct) >= schoolClass.TotalSeats)
                throw new NoSeatAvailableException();

            var hold = Hold.Create(classId, studentId, now, _options.Duration);
            await _holds.AddAsync(hold, ct);
            return ToResponse(hold, now);
        }, ct);

    public async Task<HoldResponse> GetAsync(string holdId, CancellationToken ct)
    {
        var hold = await _holds.GetAsync(holdId, ct)
                   ?? throw new NotFoundException("HOLD_NOT_FOUND", "Application not found.");
        return ToResponse(hold, _clock.UtcNow);
    }

    public Task<HoldConfirmationResponse> ConfirmAsync(string holdId, string studentId, CancellationToken ct) =>
        MutateAsync(holdId, studentId, lockStudent: true, async (hold, now) =>
        {
            // Only a live HELD hold is about to become CONFIRMED; check the student is not already confirmed in another class.
            if (hold.EffectiveStatus(now) == HoldStatus.Held)
                await EnsureNotConfirmedAsync(studentId, ct);

            hold.Confirm(now);
            return new HoldConfirmationResponse(hold.Id, hold.EffectiveStatus(now).ToApiString());
        }, ct);

    public async Task CancelAsync(string holdId, string studentId, CancellationToken ct) =>
        await MutateAsync(holdId, studentId, lockStudent: false, (hold, now) =>
        {
            hold.Cancel(now);
            return Task.FromResult(true);
        }, ct);

    public async Task<IReadOnlyList<MyHoldResponse>> ListForStudentAsync(string studentId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var rows = await _holds.ListForStudentAsync(studentId, ct);
        return rows
            .Select(r => new MyHoldResponse(
                r.Hold.Id, r.Hold.ClassId, r.ClassName, r.Hold.EffectiveStatus(now).ToApiString(), r.Hold.ExpiresAt, r.ClassShift))
            .ToList();
    }

    /// <summary>Shared path for confirm and cancel: lock the student (confirm only) and the class, reload the hold, check ownership, apply the change.</summary>
    private Task<T> MutateAsync<T>(
        string holdId, string studentId, bool lockStudent, Func<Hold, DateTime, Task<T>> change, CancellationToken ct) =>
        _tx.RunAsync(async () =>
        {
            if (lockStudent)
                await _holds.LockStudentAsync(studentId, ct);

            var classId = await _holds.GetClassIdAsync(holdId, ct)
                          ?? throw new NotFoundException("HOLD_NOT_FOUND", "Application not found.");

            await _classes.LockAsync(classId, ct);

            var hold = await _holds.GetAsync(holdId, ct)
                       ?? throw new NotFoundException("HOLD_NOT_FOUND", "Application not found.");

            if (hold.StudentId != studentId)
                throw new ForbiddenException("NOT_OWNER", "This application belongs to another student.");

            return await change(hold, _clock.UtcNow);
        }, ct);

    /// <summary>A student confirmed in one class cannot apply to, or be confirmed in, any other class.</summary>
    private async Task EnsureNotConfirmedAsync(string studentId, CancellationToken ct)
    {
        var confirmed = await _holds.FindConfirmedAsync(studentId, ct);
        if (confirmed is null)
            return;

        var shift = string.IsNullOrWhiteSpace(confirmed.ClassShift) ? "" : $" ({confirmed.ClassShift})";
        throw new ConflictException(
            "ALREADY_CONFIRMED",
            $"This birth registration number is already registered for \"{confirmed.ClassName}{shift}\" with status Confirmed, " +
            "so it cannot be used to apply to another class.");
    }

    private static HoldResponse ToResponse(Hold hold, DateTime now) =>
        new(hold.Id, hold.ClassId, hold.StudentId, hold.EffectiveStatus(now).ToApiString(), hold.ExpiresAt);
}
