namespace SchoolAdmission.Domain.Exceptions;

public sealed class NoSeatAvailableException : ConflictException
{
    public NoSeatAvailableException() : base("NO_SEATS", "No seats are available in this class.") { }
}
