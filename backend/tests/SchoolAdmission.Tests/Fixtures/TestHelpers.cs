using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Tests.Fixtures;

public static class TestHelpers
{
    /// <summary>A random 17-digit birth registration number.</summary>
    public static string NewStudentId() =>
        Random.Shared.NextInt64(10_000_000_000_000_000, 99_999_999_999_999_999).ToString();

    public static async Task<string> CreateClassAsync(HttpClient client, int totalSeats)
    {
        var response = await client.PostAsJsonAsync("/classes",
            new { name = $"Class {Guid.NewGuid():N}", shift = "Morning-Shift", totalSeats });
        response.EnsureSuccessStatusCode();
        return (await Json(response)).GetProperty("id").GetString()!;
    }

    public static HttpRequestMessage Request(HttpMethod method, string url, string? studentId)
    {
        var request = new HttpRequestMessage(method, url);
        if (studentId is not null)
            request.Headers.Add("X-Student-Id", studentId);
        return request;
    }

    public static Task<HttpResponseMessage> ApplyAsync(HttpClient client, string classId, string? studentId) =>
        client.SendAsync(Request(HttpMethod.Post, $"/classes/{classId}/holds", studentId));

    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string holdId, string? studentId) =>
        client.SendAsync(Request(HttpMethod.Post, $"/holds/{holdId}/confirm", studentId));

    public static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<JsonElement> GetClassAsync(HttpClient client, string classId) =>
        await client.GetFromJsonAsync<JsonElement>($"/classes/{classId}");
}
