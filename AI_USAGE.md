# AI Usage

## Tool used

Claude (Anthropic), in the Claude chat app.

## What it was used for

- Reading the assignment PDF and proposing the project structure (folders and empty files).
- Writing the first version of the backend (domain, services, EF Core repositories, controllers, tests), the frontend (pages, hooks, API client, tests), the Dockerfiles and `docker-compose.yml`, and a first draft of `README.md` and `ASSUMPTIONS.md`.
- Later change requests (English UI, admin login with admin-only class creation, Shift dropdown) were also implemented by Claude.
- Frontend: type-checking, unit tests and the production build were run in Claude's sandbox and passed.

## What was NOT verified by the AI

- The backend could not be compiled or tested in Claude's sandbox (no .NET SDK, no NuGet access), and the Docker setup was not run there. The C# code and `docker compose up` still need to be built and run on a machine with Docker.

## Where the AI got something wrong and how it was caught

_To be filled in by the author after building and running the project. Write only what actually happened (for example a build error, a wrong query, a test that failed) and how it was found and fixed._
