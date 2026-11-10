namespace SchoolAdmission.Application.DTOs;

public sealed record MyHoldResponse(string HoldId, string ClassId, string ClassName, string Status, DateTime ExpiresAt, string? Shift);
