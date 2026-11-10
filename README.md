# School Admission System

First-come-first-served online admission. Each class (every shift is its own class) has a limited number of seats.
Applying **holds** a seat for a short time; paying the admission fee within that time **confirms** it; otherwise the seat is released automatically.

Stack: .NET 8 (C#, EF Core) · PostgreSQL 16 · React 18 + TypeScript (Vite) · Docker

## Run

Only Docker is needed.

```bash
docker compose up --build
```

| What | URL |
|---|---|
| **Frontend (open this)** | http://localhost:3000 |
| Backend API | http://localhost:8080 (e.g. `GET /classes`, `GET /health`) |

**Admin (adds classes)**

1. Click **Admin login** at the top right and sign in. Default: username `admin`, password `admin123`.
2. In the admin section, enter a class name, choose a **Shift** (`Morning-Shift` or `Day-Shift`), enter the seats, and press **Add class**.

**Parent / student (no login)**

1. Type the student's 17-digit birth registration number in the box at the top of the page.
2. In **Classes** press **Apply**, then **Pay admission fee** or **Cancel** on the application page. **My applications** lists all of the student's applications.

Parents only see the class list and apply; they have no way to add a class.

One admission per student: once a birth registration number is **Confirmed** for a class, it cannot be used to apply to any other class; the app shows that it is already registered with status Confirmed.

Data lives in a Docker volume, so `docker compose down` then `docker compose up` loses nothing. (`docker compose down -v` deletes it.)
A database created before the Shift field existed is upgraded automatically on start.

### Configuration (all optional, see `.env.example`)

| Variable | Default | Meaning |
|---|---|---|
| `HOLD_DURATION_SECONDS` | `600` | How long a seat is held |
| `ADMIN_USERNAME` / `ADMIN_PASSWORD` | `admin` / `admin123` | Admin login. Change outside of local use |
| `ADMIN_TOKEN_SECRET` | dev value | Signs admin tokens (min 16 characters). Change outside of local use |
| `ADMIN_AUTH_REQUIRED` | `true` | `true`: `POST /classes` needs an admin token. `false`: the API endpoint is open (the UI login still works) |
| `API_URL` | `http://localhost:8080` | Backend URL as seen by the browser; read by the frontend container at start, no rebuild needed |
| `ConnectionStrings__Default` | set in compose | Backend → PostgreSQL connection |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | `postgres` / `postgres` / `school_admission` | Database credentials |

For a quick manual check of expiry: `HOLD_DURATION_SECONDS=30 docker compose up --build`.

## API notes

`POST /classes` takes `{ "name": "Class 6", "shift": "Morning-Shift", "totalSeats": 120 }`. `shift` is optional (`Morning-Shift` or `Day-Shift`); responses include it.
With `ADMIN_AUTH_REQUIRED=true` it also needs `Authorization: Bearer <token>`, where the token comes from `POST /admin/login` with `{ "username": "...", "password": "..." }`.
All read endpoints and all applicant actions (apply, confirm, cancel, my applications) need no admin token.

## Tests

### 1. API automation test + 2. Concurrency test (run these against the running app)

Black-box tests that call the real HTTP API (`tests/api/`). They need nothing but Docker.

```bash
docker compose run --rm api-tests
```

This starts the stack if it is not running, waits for it, then runs both suites:

| File | What it checks |
|---|---|
| `tests/api/automation.test.mjs` | All endpoints: required fields and status codes (201/200/204/400/401/403/404/409), validation, seat counts, confirm is idempotent, cancel, `/me/holds`, admin login, shifts, one confirmed class per student, and expiry |
| `tests/api/concurrency.test.mjs` | Many requests at the same moment: 200 students for 10 seats give exactly 10 winners; one seat goes to one applicant; several classes at once; one student sending many requests; confirm/cancel races; a student confirming two classes simultaneously ends up confirmed in one |

Run one suite only:

```bash
docker compose run --rm api-tests node --test --test-reporter=spec automation.test.mjs
docker compose run --rm api-tests node --test --test-reporter=spec concurrency.test.mjs
```

More load: `set CONCURRENCY_REQUESTS=1000` (Windows cmd) or `CONCURRENCY_REQUESTS=1000 docker compose run ...` (Linux/macOS).

**Expiry tests** need a short hold time, so by default (10 minutes) they are skipped with a note. To run them, restart the backend with a short hold, run the tests, then go back:

```bat
set HOLD_DURATION_SECONDS=3
docker compose up --build -d
docker compose run --rm api-tests
set HOLD_DURATION_SECONDS=
docker compose up -d
```

Notes: the tests add their own classes and applications to the database (they show up in the class list; `docker compose down -v` clears everything). They log in as the admin using `ADMIN_USERNAME` / `ADMIN_PASSWORD`, and expect `401` on `POST /classes` without a token unless `ADMIN_AUTH_REQUIRED=false`.
With Node 20+ installed you can also run them without Docker: `cd tests/api && node --test --test-concurrency=1` (set `API_URL=http://localhost:8080`).

### .NET tests (xunit, real API + real PostgreSQL)

```bash
docker compose run --rm tests
```

Includes `ConcurrentHoldTests` (60 simultaneous applications for 5 seats give exactly 5 successes), `AdminAuthTests`, `OneConfirmedClassTests`, expiry and unit tests.

### Frontend

```bash
cd frontend
npm install
npm test
```

## Project layout

```
backend/   src/{Domain, Application, Infrastructure, Api}  tests/SchoolAdmission.Tests (.NET tests)
tests/api/ API automation + concurrency tests (Node, run with docker compose run --rm api-tests)
frontend/  src/{api, components, pages, hooks, utils, types, config}  tests/
docker-compose.yml   ASSUMPTIONS.md   AI_USAGE.md
```

## How seats stay correct

Every seat change (apply / confirm / cancel / expiry) first takes a PostgreSQL row lock on the class (`SELECT ... FOR UPDATE`) inside a transaction.
Concurrent requests for one class therefore run one at a time, including across several backend processes sharing the database.
Details and reasoning are in `ASSUMPTIONS.md`.
