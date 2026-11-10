using System.Net;
using SchoolAdmission.Tests.Fixtures;

namespace SchoolAdmission.Tests.Integration;

public class ConcurrentHoldTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ConcurrentHoldTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Many_simultaneous_applications_never_exceed_total_seats()
    {
        const int seats = 5;
        const int attempts = 60;
        var classId = await TestHelpers.CreateClassAsync(_client, seats);

        var responses = await Task.WhenAll(Enumerable.Range(0, attempts)
            .Select(_ => Task.Run(() => TestHelpers.ApplyAsync(_client, classId, TestHelpers.NewStudentId()))));

        Assert.Equal(seats, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(attempts - seats, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var summary = await TestHelpers.GetClassAsync(_client, classId);
        Assert.Equal(seats, summary.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, summary.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, summary.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task One_student_applying_many_times_at_once_gets_a_single_seat()
    {
        var classId = await TestHelpers.CreateClassAsync(_client, 10);
        var studentId = TestHelpers.NewStudentId();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => TestHelpers.ApplyAsync(_client, classId, studentId))));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(19, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));

        var summary = await TestHelpers.GetClassAsync(_client, classId);
        Assert.Equal(1, summary.GetProperty("heldSeats").GetInt32());
    }
}
