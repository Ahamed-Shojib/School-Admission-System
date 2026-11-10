using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Auth;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Api.Controllers;

public sealed record AdminLoginRequest(string? Username, string? Password);

public sealed record AdminLoginResponse(string Token, DateTime ExpiresAt);

[ApiController]
[Route("admin")]
public sealed class AdminController : ControllerBase
{
    private readonly AdminAuthOptions _options;
    private readonly AdminTokenService _tokens;

    public AdminController(AdminAuthOptions options, AdminTokenService tokens)
    {
        _options = options;
        _tokens = tokens;
    }

    [HttpPost("login")]
    public ActionResult<AdminLoginResponse> Login([FromBody] AdminLoginRequest request)
    {
        // Both values are always compared, in constant time, so the response does not reveal which one was wrong.
        var userOk = SameSecret(request.Username, _options.Username);
        var passwordOk = SameSecret(request.Password, _options.Password);

        if (!(userOk & passwordOk))
            throw new UnauthorizedException("INVALID_CREDENTIALS", "Incorrect username or password.");

        var (token, expiresAt) = _tokens.Issue();
        return Ok(new AdminLoginResponse(token, expiresAt));
    }

    private static bool SameSecret(string? supplied, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied ?? string.Empty)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
