namespace SchoolAdmission.Application.DTOs;

public sealed record CreatedClassResponse(string Id, string Name, int TotalSeats, string? Shift);
