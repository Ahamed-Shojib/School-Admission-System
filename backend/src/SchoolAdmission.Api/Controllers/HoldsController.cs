using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Extensions;
using SchoolAdmission.Api.Filters;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public sealed class HoldsController : ControllerBase
{
    private readonly IHoldService _holds;

    public HoldsController(IHoldService holds) => _holds = holds;

    [HttpGet("{holdId}")]
    public async Task<ActionResult<HoldResponse>> Get(string holdId, CancellationToken ct) =>
        Ok(await _holds.GetAsync(holdId, ct));

    [HttpPost("{holdId}/confirm")]
    [RequireStudentId]
    public async Task<ActionResult<HoldConfirmationResponse>> Confirm(string holdId, CancellationToken ct) =>
        Ok(await _holds.ConfirmAsync(holdId, HttpContext.GetStudentId(), ct));

    [HttpDelete("{holdId}")]
    [RequireStudentId]
    public async Task<IActionResult> Cancel(string holdId, CancellationToken ct)
    {
        await _holds.CancelAsync(holdId, HttpContext.GetStudentId(), ct);
        return NoContent();
    }
}
