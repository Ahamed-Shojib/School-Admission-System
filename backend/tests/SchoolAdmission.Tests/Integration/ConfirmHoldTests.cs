using System.Net;
using SchoolAdmission.Tests.Fixtures;

namespace SchoolAdmission.Tests.Integration;

public class ConfirmHoldTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ConfirmHoldTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Confirming_twice_counts_the_seat_once()
    {
        var classId = await TestHelpers.CreateClassAsync(_client, 3);
        var studentId = TestHelpers.NewStudentId();
        var holdId = (await TestHelpers.Json(await TestHelpers.ApplyAsync(_client, classId, studentId)))
            .GetProperty("holdId").GetString()!;

        var first = await TestHelpers.ConfirmAsync(_client, holdId, studentId);
        var second = await TestHelpers.ConfirmAsync(_client, holdId, studentId);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var summary = await TestHelpers.GetClassAsync(_client, classId);
        Assert.Equal(1, summary.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, summary.GetProperty("heldSeats").GetInt32());
        Assert.Equal(2, summary.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Another_student_cannot_confirm_or_cancel_the_application()
    {
        var classId = await TestHelpers.CreateClassAsync(_client, 3);
        var owner = TestHelpers.NewStudentId();
        var other = TestHelpers.NewStudentId();
        var holdId = (await TestHelpers.Json(await TestHelpers.ApplyAsync(_client, classId, owner)))
            .GetProperty("holdId").GetString()!;

        var confirm = await TestHelpers.ConfirmAsync(_client, holdId, other);
        var cancel = await _client.SendAsync(TestHelpers.Request(HttpMethod.Delete, $"/holds/{holdId}", other));

        Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
    }

    [Fact]
    public async Task Cancel_returns_the_seat_and_the_student_can_apply_again()
    {
        var classId = await TestHelpers.CreateClassAsync(_client, 1);
        var studentId = TestHelpers.NewStudentId();
        var holdId = (await TestHelpers.Json(await TestHelpers.ApplyAsync(_client, classId, studentId)))
            .GetProperty("holdId").GetString()!;

        var cancel = await _client.SendAsync(TestHelpers.Request(HttpMethod.Delete, $"/holds/{holdId}", studentId));
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var again = await TestHelpers.ApplyAsync(_client, classId, studentId);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task Missing_or_malformed_student_id_is_a_bad_request_and_unknown_hold_is_not_found()
    {
        var classId = await TestHelpers.CreateClassAsync(_client, 1);

        var missing = await TestHelpers.ApplyAsync(_client, classId, null);
        var malformed = await TestHelpers.ApplyAsync(_client, classId, "12345");
        var unknown = await TestHelpers.ConfirmAsync(_client, "h_doesnotexist", TestHelpers.NewStudentId());

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
