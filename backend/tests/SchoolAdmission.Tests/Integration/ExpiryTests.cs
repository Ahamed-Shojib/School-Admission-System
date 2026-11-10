using System.Net.Http.Json;
using System.Net;
using SchoolAdmission.Tests.Fixtures;

namespace SchoolAdmission.Tests.Integration;

public class ExpiryTests : IClassFixture<ShortHoldApiFactory>
{
    private readonly HttpClient _client;

    public ExpiryTests(ShortHoldApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Expired_hold_frees_its_seat_by_itself_and_cannot_be_confirmed()
    {
        var classId = await TestHelpers.CreateClassAsync(_client, 1);
        var studentId = TestHelpers.NewStudentId();
        var holdId = (await TestHelpers.Json(await TestHelpers.ApplyAsync(_client, classId, studentId)))
            .GetProperty("holdId").GetString()!;

        var before = await TestHelpers.GetClassAsync(_client, classId);
        Assert.Equal(0, before.GetProperty("availableSeats").GetInt32());

        await Task.Delay(TimeSpan.FromSeconds(2.5));

        var hold = await _client.GetFromJsonAsync<System.Text.Json.JsonElement>($"/holds/{holdId}");
        Assert.Equal("EXPIRED", hold.GetProperty("status").GetString());

        var after = await TestHelpers.GetClassAsync(_client, classId);
        Assert.Equal(1, after.GetProperty("availableSeats").GetInt32());

        var confirm = await TestHelpers.ConfirmAsync(_client, holdId, studentId);
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);

        // The freed seat can be taken by someone else straight away.
        var other = await TestHelpers.ApplyAsync(_client, classId, TestHelpers.NewStudentId());
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }
}
