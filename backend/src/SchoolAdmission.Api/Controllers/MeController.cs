using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Extensions;
using SchoolAdmission.Api.Filters;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    private readonly IHoldService _holds;

    public MeController(IHoldService holds) => _holds = holds;

    [HttpGet("holds")]
    [RequireStudentId]
    public async Task<ActionResult<IReadOnlyList<MyHoldResponse>>> MyHolds(CancellationToken ct) =>
        Ok(await _holds.ListForStudentAsync(HttpContext.GetStudentId(), ct));
}
