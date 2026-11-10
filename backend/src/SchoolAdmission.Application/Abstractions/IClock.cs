namespace SchoolAdmission.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}
