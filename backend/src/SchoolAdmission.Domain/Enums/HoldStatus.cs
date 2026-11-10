namespace SchoolAdmission.Domain.Enums;

public enum HoldStatus
{
    Held,
    Confirmed,
    Expired,
    Cancelled
}

public static class HoldStatusExtensions
{
    /// <summary>API representation: HELD, CONFIRMED, EXPIRED or CANCELLED.</summary>
    public static string ToApiString(this HoldStatus status) => status.ToString().ToUpperInvariant();
}
