using SchoolAdmission.Api.Filters;

namespace SchoolAdmission.Api.Extensions;

public static class HttpContextExtensions
{
    /// <summary>The validated student id. Only valid on actions marked with [RequireStudentId].</summary>
    public static string GetStudentId(this HttpContext context) =>
        (string)context.Items[RequireStudentIdAttribute.ItemKey]!;
}
