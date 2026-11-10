namespace SchoolAdmission.Tests.Fixtures;

/// <summary>Where the integration tests find PostgreSQL. docker compose sets TEST_DB_CONNECTION for the tests service.</summary>
public static class DatabaseFixture
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=school_admission_test;Username=postgres;Password=postgres";
}
