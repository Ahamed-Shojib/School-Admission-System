using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Tests.Fixtures;

namespace SchoolAdmission.Tests.Integration;

/// <summary>A student confirmed in one class cannot apply to, or be confirmed in, any other class.</summary>
public class OneConfirmedClassTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public OneConfirmedClassTests(ApiFactory factory) => _client = factory.CreateClient();

    private async Task<string> ApplyAndGetHoldIdAsync(string classId, string studentId)
    {
        var response = await TestHelpers.ApplyAsync(_client, classId, studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await TestHelpers.Json(response)).GetProperty("holdId").GetString()!;
    }

    private async Task<string> ConfirmedStudentAsync(string classId, string studentId)
    {
        var holdId = await ApplyAndGetHoldIdAsync(classId, studentId);
        Assert.Equal(HttpStatusCode.OK, (await TestHelpers.ConfirmAsync(_client, holdId, studentId)).StatusCode);
        return holdId;
    }

    private static async Task AssertAlreadyConfirmedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await TestHelpers.Json(response);
        Assert.Equal("ALREADY_CONFIRMED", body.GetProperty("code").GetString());
        Assert.Contains("Confirmed", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Confirmed_student_cannot_apply_to_another_class()
    {
        var classA = await TestHelpers.CreateClassAsync(_client, 5);
        var classB = await TestHelpers.CreateClassAsync(_client, 5);
        var studentId = TestHelpers.NewStudentId();
        await ConfirmedStudentAsync(classA, studentId);

        await AssertAlreadyConfirmedAsync(await TestHelpers.ApplyAsync(_client, classB, studentId));

        // Class B kept all its seats, and the student only has the one application.
        var summaryB = await TestHelpers.GetClassAsync(_client, classB);
        Assert.Equal(5, summaryB.GetProperty("availableSeats").GetInt32());
        var mine = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/me/holds", studentId));
        Assert.Equal(1, (await TestHelpers.Json(mine)).GetArrayLength());
    }

    [Fact]
    public async Task Confirmed_student_cannot_apply_to_the_same_class_again_either()
    {
        var classA = await TestHelpers.CreateClassAsync(_client, 5);
        var studentId = TestHelpers.NewStudentId();
        await ConfirmedStudentAsync(classA, studentId);

        await AssertAlreadyConfirmedAsync(await TestHelpers.ApplyAsync(_client, classA, studentId));
    }

    [Fact]
    public async Task Student_holding_two_classes_can_confirm_only_one()
    {
        var classA = await TestHelpers.CreateClassAsync(_client, 5);
        var classB = await TestHelpers.CreateClassAsync(_client, 5);
        var studentId = TestHelpers.NewStudentId();
        var holdA = await ApplyAndGetHoldIdAsync(classA, studentId);
        var holdB = await ApplyAndGetHoldIdAsync(classB, studentId); // holding is still allowed: nothing is confirmed yet

        Assert.Equal(HttpStatusCode.OK, (await TestHelpers.ConfirmAsync(_client, holdA, studentId)).StatusCode);
        await AssertAlreadyConfirmedAsync(await TestHelpers.ConfirmAsync(_client, holdB, studentId));

        var hold = await _client.GetFromJsonAsync<JsonElement>($"/holds/{holdB}");
        Assert.Equal("HELD", hold.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Two_simultaneous_confirms_for_one_student_confirm_exactly_one_class()
    {
        var classA = await TestHelpers.CreateClassAsync(_client, 5);
        var classB = await TestHelpers.CreateClassAsync(_client, 5);
        var studentId = TestHelpers.NewStudentId();
        var holdA = await ApplyAndGetHoldIdAsync(classA, studentId);
        var holdB = await ApplyAndGetHoldIdAsync(classB, studentId);

        var results = await Task.WhenAll(
            Task.Run(() => TestHelpers.ConfirmAsync(_client, holdA, studentId)),
            Task.Run(() => TestHelpers.ConfirmAsync(_client, holdB, studentId)));

        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, results.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var confirmedSeats = (await TestHelpers.GetClassAsync(_client, classA)).GetProperty("confirmedSeats").GetInt32()
                             + (await TestHelpers.GetClassAsync(_client, classB)).GetProperty("confirmedSeats").GetInt32();
        Assert.Equal(1, confirmedSeats);
    }

    [Fact]
    public async Task Other_students_and_unconfirmed_students_are_not_affected()
    {
        var classA = await TestHelpers.CreateClassAsync(_client, 5);
        var classB = await TestHelpers.CreateClassAsync(_client, 5);
        var confirmedStudent = TestHelpers.NewStudentId();
        await ConfirmedStudentAsync(classA, confirmedStudent);

        // A different student can still apply to either class.
        var other = TestHelpers.NewStudentId();
        Assert.Equal(HttpStatusCode.Created, (await TestHelpers.ApplyAsync(_client, classA, other)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await TestHelpers.ApplyAsync(_client, classB, other)).StatusCode);
    }
}
