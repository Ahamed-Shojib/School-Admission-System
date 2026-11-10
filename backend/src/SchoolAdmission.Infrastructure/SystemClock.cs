using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
