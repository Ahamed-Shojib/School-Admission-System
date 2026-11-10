namespace SchoolAdmission.Api.Auth;

public sealed class AdminAuthOptions
{
    public string Username { get; init; } = "admin";
    public string Password { get; init; } = "admin123";

    /// <summary>Key used to sign admin tokens. Change it outside of local use.</summary>
    public string TokenSecret { get; init; } = "";

    public TimeSpan TokenLifetime { get; init; } = TimeSpan.FromHours(8);

    /// <summary>When true, POST /classes needs a valid admin token. When false the endpoint is open (the login still works).</summary>
    public bool Required { get; init; } = true;
}
