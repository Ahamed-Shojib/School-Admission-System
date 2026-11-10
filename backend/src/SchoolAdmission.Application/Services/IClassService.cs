using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Application.Services;

public interface IClassService
{
    Task<CreatedClassResponse> CreateAsync(CreateClassRequest request, CancellationToken ct);
    Task<IReadOnlyList<ClassResponse>> ListAsync(CancellationToken ct);
    Task<ClassResponse> GetAsync(string classId, CancellationToken ct);
}
