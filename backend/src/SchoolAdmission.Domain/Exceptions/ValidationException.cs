namespace SchoolAdmission.Domain.Exceptions;

public class ValidationException : DomainException
{
    public ValidationException(string code, string message) : base(code, message) { }
}
