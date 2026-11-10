namespace SchoolAdmission.Domain.Exceptions;

public class ForbiddenException : DomainException
{
    public ForbiddenException(string code, string message) : base(code, message) { }
}
