# syntax=docker/dockerfile:1

# ---- restore: copy only project files first so the package restore layer is cached ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src
COPY src/SchoolAdmission.Domain/SchoolAdmission.Domain.csproj src/SchoolAdmission.Domain/
COPY src/SchoolAdmission.Application/SchoolAdmission.Application.csproj src/SchoolAdmission.Application/
COPY src/SchoolAdmission.Infrastructure/SchoolAdmission.Infrastructure.csproj src/SchoolAdmission.Infrastructure/
COPY src/SchoolAdmission.Api/SchoolAdmission.Api.csproj src/SchoolAdmission.Api/
COPY tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj tests/SchoolAdmission.Tests/
# The test project references the Api project, which references everything else, so this restores all of them.
RUN dotnet restore tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj
COPY . .

# ---- test: used by `docker compose run --rm tests` ----
FROM restore AS test
ENTRYPOINT ["dotnet", "test", "tests/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj", "--no-restore", "--logger", "console;verbosity=normal"]

# ---- publish ----
FROM restore AS publish
RUN dotnet publish src/SchoolAdmission.Api/SchoolAdmission.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime (default target) ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]
