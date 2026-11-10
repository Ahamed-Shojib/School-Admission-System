namespace SchoolAdmission.Domain.Exceptions;

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string code, string message) : base(code, message) { }
}
