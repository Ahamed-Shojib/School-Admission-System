namespace SchoolAdmission.Application.DTOs;

/// <summary>Shift is optional so existing clients that send only name and totalSeats keep working.</summary>
public sealed record CreateClassRequest(string? Name, int TotalSeats, string? Shift = null);
