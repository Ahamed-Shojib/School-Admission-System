namespace SchoolAdmission.Domain.Exceptions;

/// <summary>Base type for expected, user-facing business errors. Carries a stable machine-readable code.</summary>
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message) => Code = code;
}
