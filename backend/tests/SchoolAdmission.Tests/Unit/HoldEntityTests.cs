using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Tests.Unit;

public class HoldEntityTests
{
    private static readonly DateTime Start = new(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc);
    private const string StudentId = "20152691234567890";

    private static Hold NewHold() => Hold.Create("c_1", StudentId, Start, TimeSpan.FromMinutes(10));

    [Fact]
    public void New_hold_is_held_until_its_deadline()
    {
        var hold = NewHold();

        Assert.Equal(Start.AddMinutes(10), hold.ExpiresAt);
        Assert.Equal(HoldStatus.Held, hold.EffectiveStatus(Start.AddMinutes(9)));
        Assert.Equal(HoldStatus.Expired, hold.EffectiveStatus(Start.AddMinutes(10)));
    }

    [Fact]
    public void Confirm_after_deadline_is_rejected()
    {
        var hold = NewHold();

        var ex = Assert.Throws<ConflictException>(() => hold.Confirm(Start.AddMinutes(11)));
        Assert.Equal("HOLD_EXPIRED", ex.Code);
    }

    [Fact]
    public void Confirm_is_idempotent()
    {
        var hold = NewHold();

        hold.Confirm(Start.AddMinutes(1));
        hold.Confirm(Start.AddMinutes(2));

        Assert.Equal(HoldStatus.Confirmed, hold.Status);
        Assert.Equal(Start.AddMinutes(1), hold.ConfirmedAt);
    }

    [Fact]
    public void Cancelled_hold_cannot_be_confirmed_and_confirmed_hold_cannot_be_cancelled()
    {
        var cancelled = NewHold();
        cancelled.Cancel(Start.AddMinutes(1));
        Assert.Throws<ConflictException>(() => cancelled.Confirm(Start.AddMinutes(2)));

        var confirmed = NewHold();
        confirmed.Confirm(Start.AddMinutes(1));
        Assert.Throws<ConflictException>(() => confirmed.Cancel(Start.AddMinutes(2)));
    }
}
