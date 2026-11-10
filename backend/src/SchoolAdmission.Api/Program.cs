using SchoolAdmission.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAdmissionApi(builder.Configuration);

var app = builder.Build();

app.UseAdmissionPipeline();
await app.InitializeDatabaseAsync();

app.Run();

// Makes the entry point visible to WebApplicationFactory in the integration tests.
public partial class Program { }
