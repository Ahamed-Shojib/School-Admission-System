using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Application.Services;

public interface IHoldService
{
    Task<HoldResponse> CreateAsync(string classId, string studentId, CancellationToken ct);
    Task<HoldResponse> GetAsync(string holdId, CancellationToken ct);
    Task<HoldConfirmationResponse> ConfirmAsync(string holdId, string studentId, CancellationToken ct);
    Task CancelAsync(string holdId, string studentId, CancellationToken ct);
    Task<IReadOnlyList<MyHoldResponse>> ListForStudentAsync(string studentId, CancellationToken ct);
}
