using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Extensions;
using SchoolAdmission.Api.Filters;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public sealed class ClassesController : ControllerBase
{
    private readonly IClassService _classes;
    private readonly IHoldService _holds;

    public ClassesController(IClassService classes, IHoldService holds)
    {
        _classes = classes;
        _holds = holds;
    }

    [HttpPost]
    [RequireAdmin]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request, CancellationToken ct)
    {
        var created = await _classes.CreateAsync(request, ct);
        return Created($"/classes/{created.Id}", created);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> List(CancellationToken ct) =>
        Ok(await _classes.ListAsync(ct));

    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassResponse>> Get(string classId, CancellationToken ct) =>
        Ok(await _classes.GetAsync(classId, ct));

    [HttpPost("{classId}/holds")]
    [RequireStudentId]
    public async Task<IActionResult> Apply(string classId, CancellationToken ct)
    {
        var hold = await _holds.CreateAsync(classId, HttpContext.GetStudentId(), ct);
        return Created($"/holds/{hold.HoldId}", hold);
    }
}
