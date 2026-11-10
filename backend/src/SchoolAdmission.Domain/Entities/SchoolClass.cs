using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Domain.Entities;

/// <summary>A class with a limited seat count. Every shift is its own class.</summary>
public sealed class SchoolClass
{
    public const int MaxNameLength = 100;
    public const int MaxSeats = 100_000;

    public string Id { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Shift { get; private set; }
    public int TotalSeats { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private SchoolClass() { }

    public static SchoolClass Create(string? name, string? shift, int totalSeats, DateTime now)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ValidationException("CLASS_NAME_REQUIRED", "Class name is required.");
        if (trimmed.Length > MaxNameLength)
            throw new ValidationException("CLASS_NAME_TOO_LONG", $"Class name must be at most {MaxNameLength} characters.");
        if (totalSeats < 1 || totalSeats > MaxSeats)
            throw new ValidationException("INVALID_TOTAL_SEATS", $"totalSeats must be between 1 and {MaxSeats}.");

        var normalizedShift = ClassShifts.Normalize(shift);

        return new SchoolClass
        {
            Id = "c_" + Guid.NewGuid().ToString("N")[..12],
            Name = trimmed,
            Shift = normalizedShift,
            TotalSeats = totalSeats,
            CreatedAt = now
        };
    }
}
