using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Tests.Fixtures;

namespace SchoolAdmission.Tests.Integration;

public class AdminAuthTests : IClassFixture<AdminApiFactory>
{
    private readonly HttpClient _client;

    public AdminAuthTests(AdminApiFactory factory) => _client = factory.CreateClient();

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/admin/login",
            new { username = ApiFactory.AdminUsername, password = ApiFactory.AdminPassword });
        response.EnsureSuccessStatusCode();
        return (await TestHelpers.Json(response)).GetProperty("token").GetString()!;
    }

    private Task<HttpResponseMessage> CreateClassAsync(object body, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/classes") { Content = JsonContent.Create(body) };
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Creating_a_class_without_a_token_is_unauthorized()
    {
        var response = await CreateClassAsync(new { name = "Class 6", shift = "Day-Shift", totalSeats = 10 }, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("ADMIN_AUTH_REQUIRED", (await TestHelpers.Json(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/admin/login",
            new { username = ApiFactory.AdminUsername, password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("INVALID_CREDENTIALS", (await TestHelpers.Json(response)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_tampered_or_garbage_token_is_rejected()
    {
        var good = await LoginAsync();
        var tampered = good[..^2] + (good.EndsWith("AA") ? "BB" : "AA");

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await CreateClassAsync(new { name = "Class 6", totalSeats = 10 }, tampered)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await CreateClassAsync(new { name = "Class 6", totalSeats = 10 }, "not-a-token")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_a_class_with_a_shift_and_everyone_can_read_it()
    {
        var token = await LoginAsync();

        var created = await CreateClassAsync(new { name = "Class 6", shift = "day-shift", totalSeats = 40 }, token);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var body = await TestHelpers.Json(created);
        Assert.Equal("Day-Shift", body.GetProperty("shift").GetString());
        var classId = body.GetProperty("id").GetString()!;

        // No token needed to read.
        var anonymous = await _client.GetFromJsonAsync<JsonElement>($"/classes/{classId}");
        Assert.Equal("Day-Shift", anonymous.GetProperty("shift").GetString());
        Assert.Equal("Class 6", anonymous.GetProperty("name").GetString());
        Assert.Equal(40, anonymous.GetProperty("availableSeats").GetInt32());
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/classes")).StatusCode);
    }

    [Fact]
    public async Task Unknown_shift_is_a_bad_request_and_shift_can_be_left_out()
    {
        var token = await LoginAsync();

        var invalid = await CreateClassAsync(new { name = "Class 6", shift = "Night-Shift", totalSeats = 10 }, token);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("INVALID_SHIFT", (await TestHelpers.Json(invalid)).GetProperty("code").GetString());

        // Existing clients that send only name and totalSeats still work.
        var withoutShift = await CreateClassAsync(new { name = "Class 7", totalSeats = 10 }, token);
        Assert.Equal(HttpStatusCode.Created, withoutShift.StatusCode);
    }

    [Fact]
    public async Task Applying_for_a_seat_never_needs_an_admin_token()
    {
        var token = await LoginAsync();
        var created = await CreateClassAsync(new { name = "Class 8", shift = "Morning-Shift", totalSeats = 2 }, token);
        var classId = (await TestHelpers.Json(created)).GetProperty("id").GetString()!;

        var apply = await TestHelpers.ApplyAsync(_client, classId, TestHelpers.NewStudentId());

        Assert.Equal(HttpStatusCode.Created, apply.StatusCode);
    }
}
