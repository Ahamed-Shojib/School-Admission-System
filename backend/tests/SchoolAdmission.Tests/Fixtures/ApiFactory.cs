using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SchoolAdmission.Tests.Fixtures;

/// <summary>Boots the real API against the test database. The schema is created on startup like in production.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "test-password";

    protected virtual int HoldDurationSeconds => 600;

    /// <summary>Off by default so the seat and expiry tests can create classes freely; AdminApiFactory turns it on.</summary>
    protected virtual bool AdminAuthRequired => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", DatabaseFixture.ConnectionString);
        builder.UseSetting("HOLD_DURATION_SECONDS", HoldDurationSeconds.ToString());
        builder.UseSetting("ADMIN_AUTH_REQUIRED", AdminAuthRequired ? "true" : "false");
        builder.UseSetting("ADMIN_USERNAME", AdminUsername);
        builder.UseSetting("ADMIN_PASSWORD", AdminPassword);
        builder.UseSetting("ADMIN_TOKEN_SECRET", "test-secret-test-secret-test-secret");
    }
}

/// <summary>Holds last one second, so expiry can be observed in a test.</summary>
public sealed class ShortHoldApiFactory : ApiFactory
{
    protected override int HoldDurationSeconds => 1;
}

/// <summary>Admin protection on, as in the default docker setup.</summary>
public sealed class AdminApiFactory : ApiFactory
{
    protected override bool AdminAuthRequired => true;
}
