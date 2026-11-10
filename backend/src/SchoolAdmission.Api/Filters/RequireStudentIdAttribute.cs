using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Api.Filters;

/// <summary>Requires the X-Student-Id header to hold a 17-digit birth registration number.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed partial class RequireStudentIdAttribute : ActionFilterAttribute
{
    public const string HeaderName = "X-Student-Id";
    public const string ItemKey = "StudentId";

    [GeneratedRegex(@"^\d{17}$")]
    private static partial Regex SeventeenDigits();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var value = context.HttpContext.Request.Headers[HeaderName].ToString().Trim();

        if (value.Length == 0)
            throw new ValidationException("STUDENT_ID_REQUIRED", $"The {HeaderName} header is required.");
        if (!SeventeenDigits().IsMatch(value))
            throw new ValidationException("STUDENT_ID_INVALID", $"The {HeaderName} header must be a 17-digit number.");

        context.HttpContext.Items[ItemKey] = value;
    }
}
