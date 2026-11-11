# Assumptions

The brief leaves many details open. These are the choices made, with the reason.

## Seats and concurrency

1. **A seat is "used" by a HELD hold that has not passed its deadline, or by a CONFIRMED hold.** `heldSeats + confirmedSeats <= totalSeats` always. `availableSeats = totalSeats - heldSeats - confirmedSeats`.
2. **Concurrency control is a row lock on the class** (`SELECT ... FOR UPDATE` in a transaction), not an in-process lock. Reason: an in-process lock (C# `lock`, `SemaphoreSlim`) only works inside one process. The row lock is enforced by PostgreSQL, so it also holds when two backend processes share one database (the bonus case), and different classes do not block each other.
3. **Inside the lock** the order is: release this class's overdue holds, reject if the student already has a live application, count live holds, insert. Confirm and cancel take the same lock, so they cannot interleave with a booking.
4. **A filtered unique index** on `(ClassId, StudentId)` where status is HELD or CONFIRMED is a second line of defence for "one active application per student per class".

## Expiry

5. **Expiry works without any API call, two ways.** (a) Every read treats a HELD hold with `expiresAt <= now` as EXPIRED and does not count it as a used seat. (b) A background worker (every 5 s) persists `EXPIRED` for overdue rows, class by class under the class lock. Correctness depends only on (a); (b) is housekeeping.
6. **Time source** is the backend's UTC clock; `expiresAt` is sent in ISO-8601 UTC. The UI counts down against the browser clock, so a badly wrong device clock can show a slightly different countdown, but the server decides: confirm after the deadline is always rejected.
7. **`HOLD_DURATION_SECONDS`** must be a positive integer; the app refuses to start otherwise.

## Student identity

8. **No login for students or parents.** (Admins do log in, see below.) `X-Student-Id` must be exactly 17 digits. Missing → `400 STUDENT_ID_REQUIRED`, malformed → `400 STUDENT_ID_INVALID`. The number is not checked against any registry.
9. **The header is required** on apply, confirm, cancel and `/me/holds`. It is not required on read-only `GET /classes`, `GET /classes/{id}` or `GET /holds/{id}`, as the brief lists it only on the others. Anyone knowing a hold id can read its status; only the owner can change it.
10. **Ownership:** confirming or cancelling someone else's hold → `403 NOT_OWNER`.
11. **One live application per student per class** (HELD or CONFIRMED). A second attempt in the same class → `409 ALREADY_APPLIED`. A student may hold seats in several different classes at the same time, but see the next point once one is confirmed. After cancel or expiry the student may apply to the same class again.
11a. **One confirmed class per student, across all classes.** Once a birth registration number is CONFIRMED in a class, it cannot apply to any other class (or to the same class again), and a hold it still has elsewhere cannot be confirmed. Both return `409 ALREADY_CONFIRMED`, and the message names the class, for example: *This birth registration number is already registered for "Class 6 (Morning-Shift)" with status Confirmed, so it cannot be used to apply to another class.* This is stricter than the brief, which only limits a student to one active application per class; it was added on request.
11b. **How it stays correct under simultaneous requests:** apply and confirm first take a PostgreSQL advisory lock for that student, then the class lock (always in that order, so requests cannot deadlock). Two simultaneous confirms for one student in two classes therefore confirm exactly one. A unique index on the student for CONFIRMED holds is a backstop. If old data already broke the rule the index cannot be built; this is logged and the app still starts.
11c. **Holding is not blocked by another hold,** only by a confirmed admission. Seats the student holds in other classes are not auto-released when one is confirmed; they simply cannot be confirmed and expire after `HOLD_DURATION_SECONDS` (or the student cancels them).

## API behaviour not specified

12. **Status codes:** create class `201`; apply `201`; no seat `409 NO_SEATS`; unknown class/hold `404`; invalid input `400`; wrong owner `403`; business-rule conflicts `409`; cancel success `204`.
13. **Error body:** `{ "code": "...", "message": "..." }`. The UI maps `code` to an English message and never shows the raw text.
14. **Confirm** is idempotent: confirming a CONFIRMED hold returns `200` and changes nothing (the seat is counted once). Confirming an expired hold → `409 HOLD_EXPIRED`; a cancelled one → `409 INVALID_STATE`.
15. **Cancel** is allowed only while HELD. Cancelling an already CANCELLED hold returns `204` (idempotent); EXPIRED → `409 HOLD_EXPIRED`; CONFIRMED → `409 INVALID_STATE` (an admission, once paid for, is not cancelled through this flow; refunds are out of scope).
16. **`GET /holds/{id}` and `/me/holds`** report the effective status, so an overdue hold reads `EXPIRED` immediately.
17. **`/me/holds`** returns all of the student's applications, newest first, including expired and cancelled ones.
18. **Class creation:** name is trimmed, 1–100 characters; `totalSeats` is an integer from 1 to 100,000. Duplicate names are allowed (admins may reuse a name). There is no edit or delete of a class, as the brief has none.
19. **IDs** are short prefixed random strings (`c_...`, `h_...`).
20. **Response extras:** hold responses also include `studentId` (the brief allows extra fields; no listed field is removed or renamed).

## Persistence and operations

21. **Schema creation** uses EF Core `EnsureCreated` on startup (retrying while the database starts), with no migration history. Simple for a take-home; a real system would use migrations.
22. **PostgreSQL data** is in a named Docker volume, so restarting containers keeps all data.
23. **Ports:** backend `8080` as required; frontend `3000`.
24. **CORS** allows any origin, because the frontend (port 3000) calls the backend (port 8080) from the browser and there is no authentication to protect.

## Frontend

25. **The backend URL is runtime config.** The frontend container writes `config.js` from `API_URL` at start, so changing it needs a restart, not a rebuild.
26. **The student number is kept in `localStorage`**, so refresh or reopening the browser keeps it and the student sees their own applications again.
27. **UI language is English** (all screens, buttons and error messages). The buttons are "Apply", "Pay admission fee" and "Cancel".
28. **Updates without reload:** the class list refreshes every 3 s, an open application every 2 s (stopping once it can no longer change), "my applications" every 3 s. The countdown ticks locally, and an application is shown as EXPIRED the moment its deadline passes, with the pay button disabled.
29. **"ভর্তি ফি দিন" = payment succeeded**, as stated in the brief; no payment gateway or amount.
30. **Routing** uses URL hashes (`#/classes`, `#/me`, `#/holds/{id}`): a refresh keeps the user on the same page without any server rewrite rules.
31. **Adding a class is admin-only.** See "Admin and shifts" below.

## Admin and shifts

32. **Admin login:** a button at the top right of every page opens the admin login. The credentials come from `ADMIN_USERNAME` / `ADMIN_PASSWORD` (defaults `admin` / `admin123`, for local use only). There is one admin account, no user management and no password change screen.
33. **Admin-only means enforced on the backend too, not just hidden in the UI.** `POST /classes` needs `Authorization: Bearer <token>`; without a valid token it returns `401 ADMIN_AUTH_REQUIRED`. The UI shows the Add Class form only in the admin section.
34. **Tokens** are stateless, HMAC-signed (key `ADMIN_TOKEN_SECRET`) and valid for 8 hours. Being stateless, every backend process accepts the same tokens. The UI keeps the token in `sessionStorage`, so a refresh keeps the admin signed in but closing the tab signs out, and an expired token signs the UI out automatically. There is no server-side logout or revocation; "Log out" removes the token from the browser.
35. **`ADMIN_AUTH_REQUIRED=false` switch.** The assignment's automated API tests call `POST /classes` directly with no credentials. Setting this to `false` leaves that endpoint open (the UI login then only gates the screen), so the original API contract holds exactly. Default is `true`.
36. **Everything else needs no admin token:** the class list, class details, applications and all applicant actions. Parents only see the list and apply; they have no way to add a class.
37. **Shift** is a dropdown with exactly `Morning-Shift` and `Day-Shift`, stored on the class. Each shift is still its own class with its own seats (a "Class 6" in each shift is two classes), as the brief describes.
38. **`name` stays exactly what the admin typed** (for example `Class 6`), and the shift is a separate `shift` field, shown in its own column. This keeps the brief's `name` field unchanged.
39. **`shift` is optional in the API** (a request with only `name` and `totalSeats` still works and gives `shift: null`), so the original contract is unchanged. A value that is not one of the two shifts (case-insensitive) → `400 INVALID_SHIFT`. The UI always requires a shift.
40. **Existing databases** get the new `Shift` column automatically on start (`ALTER TABLE ... ADD COLUMN IF NOT EXISTS`); existing classes have no shift and show "—".

## Bonus

41. **Two backend processes on one database** keep `held + confirmed <= totalSeats` because of assumption 2: the lock is in PostgreSQL, not in the application. The worker also takes the same lock, so several processes running it do not conflict.
