using SchoolAdmission.Domain;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Tests.Unit;

public class SchoolClassTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 4, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("Morning-Shift", "Morning-Shift")]
    [InlineData("day-shift", "Day-Shift")]
    [InlineData("  DAY-SHIFT ", "Day-Shift")]
    public void Known_shifts_are_stored_in_their_canonical_spelling(string input, string expected)
    {
        var created = SchoolClass.Create("Class 6", input, 120, Now);

        Assert.Equal(expected, created.Shift);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Shift_is_optional(string? input)
    {
        Assert.Null(SchoolClass.Create("Class 6", input, 120, Now).Shift);
    }

    [Fact]
    public void Unknown_shift_is_rejected()
    {
        var ex = Assert.Throws<ValidationException>(() => SchoolClass.Create("Class 6", "Night-Shift", 120, Now));

        Assert.Equal("INVALID_SHIFT", ex.Code);
    }

    [Fact]
    public void The_dropdown_values_are_exactly_morning_and_day()
    {
        Assert.Equal(new[] { "Morning-Shift", "Day-Shift" }, ClassShifts.All);
    }
}
