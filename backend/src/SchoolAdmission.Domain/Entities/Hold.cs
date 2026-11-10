using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

/// <summary>A student's application for a seat: held temporarily, then confirmed, expired or cancelled.</summary>
public sealed class Hold
{
    public string Id { get; private set; } = default!;
    public string ClassId { get; private set; } = default!;
    public string StudentId { get; private set; } = default!;
    public HoldStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private Hold() { }

    public static Hold Create(string classId, string studentId, DateTime now, TimeSpan duration) => new()
    {
        Id = "h_" + Guid.NewGuid().ToString("N")[..12],
        ClassId = classId,
        StudentId = studentId,
        Status = HoldStatus.Held,
        CreatedAt = now,
        ExpiresAt = now + duration
    };

    /// <summary>
    /// A HELD hold past its deadline is EXPIRED even if the background job has not persisted that yet,
    /// so seats and statuses are right the instant time runs out, with no API call needed.
    /// </summary>
    public HoldStatus EffectiveStatus(DateTime now) =>
        Status == HoldStatus.Held && ExpiresAt <= now ? HoldStatus.Expired : Status;

    /// <summary>Idempotent: confirming an already confirmed hold changes nothing.</summary>
    public void Confirm(DateTime now)
    {
        switch (EffectiveStatus(now))
        {
            case HoldStatus.Confirmed:
                return;
            case HoldStatus.Held:
                Status = HoldStatus.Confirmed;
                ConfirmedAt = now;
                return;
            case HoldStatus.Expired:
                throw new ConflictException("HOLD_EXPIRED", "This application has expired.");
            default:
                throw new ConflictException("INVALID_STATE", "This application was cancelled and cannot be confirmed.");
        }
    }

    /// <summary>Idempotent for cancelled holds. Only a live HELD hold can be cancelled.</summary>
    public void Cancel(DateTime now)
    {
        switch (EffectiveStatus(now))
        {
            case HoldStatus.Cancelled:
                return;
            case HoldStatus.Held:
                Status = HoldStatus.Cancelled;
                return;
            case HoldStatus.Expired:
                throw new ConflictException("HOLD_EXPIRED", "This application has already expired.");
            default:
                throw new ConflictException("INVALID_STATE", "A confirmed admission cannot be cancelled.");
        }
    }
}
