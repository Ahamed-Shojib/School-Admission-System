namespace SchoolAdmission.Application.DTOs;

public sealed record HoldResponse(string HoldId, string ClassId, string StudentId, string Status, DateTime ExpiresAt);
