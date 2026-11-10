namespace SchoolAdmission.Application.Options;

public sealed class HoldOptions
{
    public const int DefaultDurationSeconds = 600;

    public int DurationSeconds { get; init; } = DefaultDurationSeconds;

    public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);
}
