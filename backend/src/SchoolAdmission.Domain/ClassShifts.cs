using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain;

/// <summary>The shifts a class can run in. Each shift of a class is created as its own class with its own seats.</summary>
public static class ClassShifts
{
    public const string Morning = "Morning-Shift";
    public const string Day = "Day-Shift";

    public static readonly IReadOnlyList<string> All = new[] { Morning, Day };

    /// <summary>Null or blank means "no shift given". Otherwise the value must be a known shift (case-insensitive) and is returned in its canonical spelling.</summary>
    public static string? Normalize(string? shift)
    {
        if (string.IsNullOrWhiteSpace(shift))
            return null;

        var match = All.FirstOrDefault(s => string.Equals(s, shift.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? throw new ValidationException("INVALID_SHIFT", $"Shift must be one of: {string.Join(", ", All)}.");
    }
}
