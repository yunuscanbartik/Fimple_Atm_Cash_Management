# ATM Cash Management Backend

Backend for the cash management of a bank's ATM network, built as an ASP.NET Core REST API on .NET 10 with EF Core (code first, PostgreSQL). It covers the core rules of Part A and all of Part B (B1–B10) of the assignment.

- [How to run the project and its tests](#how-to-run-the-project-and-its-tests)
- [Architecture and key design decisions](#architecture-and-key-design-decisions)
- [Assumptions](#assumptions)
- [Unfinished items and known limitations](#unfinished-items-and-known-limitations)
- [AI tools](#ai-tools)

## How to run the project and its tests

### With Docker (API, worker and database)

```bash
docker compose up --build
```

| What | Where |
|---|---|
| Swagger UI | http://localhost:8080/swagger (the root URL redirects here) |
| OpenAPI document | http://localhost:8080/openapi/v1.json |
| Health (readiness, checks PostgreSQL) | http://localhost:8080/health |
| Health (liveness) | http://localhost:8080/health/live |
| PostgreSQL from the host | `localhost:5433`, database `fimple_atm`, user `postgres` |

The API applies the EF Core migrations on startup, so there is nothing to prepare. The worker starts after the API reports healthy. The password and the card-hashing key have local defaults in `docker-compose.yml`; copy `.env.example` to `.env` to change them (keep the password to letters, digits, dashes and underscores because it is placed in a connection string).

### Without Docker

You need the .NET 10 SDK and a PostgreSQL server. Your database password goes into `appsettings.Local.json` in the repository root. The file is git-ignored; create it from the committed example and replace the password:

```bash
cp appsettings.Local.example.json appsettings.Local.json
```

In the Development environment the API, the worker and the tests find this file automatically and it overrides `appsettings.Development.json`, which only holds a `CHANGE_ME` placeholder. An environment variable such as `ConnectionStrings__PostgreSql` still wins over both.

```bash
dotnet run --project Fimple.Atm.Api --launch-profile http      # http://localhost:5272/swagger
dotnet run --project Fimple.Atm.Worker                          # quote expiry and outbox
```

On startup the API applies the migrations and creates the database if it does not exist (the user needs the right to create databases). The development card-hashing key comes from `appsettings.Development.json`. Outside Development, `CardHashing__Key` (at least 32 characters) must be provided or the application refuses to start.

### Tests

```bash
dotnet test
```

The suite has 55 tests: 21 unit tests (the note selector, including the eight B3 acceptance cases, and the deposit allocator) and 34 integration tests that run against a real PostgreSQL database.

The integration tests create and drop their own database, `fimple_atm_test`, boot the API in-process with `WebApplicationFactory` and a fake clock, and drive it over HTTP. They take the connection string from the `FIMPLE_TEST_CONNECTION_STRING` environment variable, or from `appsettings.Local.json` (see above). The user needs the right to create databases. For example, against the compose database:

```bash
docker compose up -d postgres
FIMPLE_TEST_CONNECTION_STRING="Host=localhost;Port=5433;Username=postgres;Password=fimple-local-dev-password" dotnet test
```

### Postman

Import `postman/ATM_Cash_Management.postman_collection.json` and one of the environments (`ATM_docker_compose` for port 8080, `ATM_dotnet_run` for port 5272), then run the collection in order with the Collection Runner. Folder 1 is the scenario from the assignment: create ATM, configure cassettes, load cash, put in service, request quote, confirm, deposit, switch to maintenance, reconcile, report. Folder 2 covers the four-eyes approval, folder 3 the alternatives, cancellation and error cases. Every request has tests, and ids are passed along through collection variables.

### Endpoints

| Method and path | Purpose |
|---|---|
| `GET /api/atms`, `GET /api/atms/{id}` | List ATMs; one ATM with cassettes, note counts per denomination and totals per currency (A12) |
| `POST /api/atms` | Create an ATM (`UNCONFIGURED`) |
| `PUT /api/atms/{id}/cassettes` | Configure the five cassettes |
| `POST /api/atms/{id}/load`, `/unload` | Record cash loaded or removed by hand |
| `POST /api/atms/{id}/put-in-service`, `/enter-maintenance` | State transitions |
| `POST /api/atms/{id}/withdrawals/quotes` | Quote a withdrawal and reserve the offered notes for 60 seconds |
| `GET /api/atms/{id}/withdrawals/quotes/{quoteId}` | Read a quote |
| `POST /api/atms/{id}/withdrawals/quotes/{quoteId}/confirm` | Confirm (header `Idempotency-Key` required) |
| `POST /api/atms/{id}/withdrawals/quotes/{quoteId}/cancel` | Cancel and release the reservation |
| `POST /api/atms/{id}/deposits` | Deposit notes |
| `GET/POST /api/atms/{id}/reconciliations`, `GET .../{rid}`, `POST .../{rid}/approve` | Reconciliation and four-eyes approval (header `X-User`) |
| `GET /api/atms/{id}/consistency` | Compare stored balances with the movement ledger (B8) |
| `GET /api/reports/operations` | Operations report, optional `?atmId=` (B10) |

Enums use upper snake case in JSON (`TRY`, `IN_SERVICE`, `RECONCILIATION_ADJUSTMENT`); denominations are plain numbers (`20`, `50`, `100`, `500`).

## Architecture and key design decisions

### Structure

The layout follows the Dashboard API of the pinqponq server (separate Api, Application, Domain, Infrastructure and Worker projects; `XModel.Action.Request` / `.Return` classes; `IXService` interfaces in Domain; one `ConfigureXEntity` method per entity; `t_` table prefix; extension methods for startup), with the places where the assignment asks for something different (ProblemDetails, REST routes, `X-User` instead of JWT, real-database tests, Swagger) following the assignment.

```
Fimple.Atm.Api            controllers, exception middleware, OpenAPI, health checks
Fimple.Atm.Application    services, note selector, deposit allocator, movement recorder
Fimple.Atm.Domain         entities, enums, rule constants, models, interfaces, exceptions
Fimple.Atm.Infrastructure AppDbContext, EF Core migrations, PostgreSQL helpers
Fimple.Atm.Worker         quote expiry worker and outbox worker (BackgroundService)
Fimple.Atm.Tests          unit and integration tests
```

### Note selection (B3)

A greedy algorithm that starts with the largest denomination gives wrong answers for some of the B3 cases, so the selector is a bounded dynamic program over the available notes. Denominations are grouped (two cassettes with the same denomination count as one stock), amounts are expressed in units of the greatest common divisor of the denominations, and `minimumNotes[amount]` is built denomination by denomination with the note count limited by what is in stock. The exact breakdown with the fewest notes comes straight from the table. When the amount cannot be paid, the same table is scanned downwards and upwards for the nearest payable amounts, which gives the lower and upper alternatives. The upper alternative is dropped by the service when it would exceed the card's remaining daily limit.

After the denomination counts are known, notes of one denomination are taken from the cassette with the higher fill ratio first (B2). It is a pure function, covered by the eight acceptance cases of the assignment plus extra cases.

### Concurrency (B6)

Every operation that changes an ATM runs in one database transaction that starts by locking the ATM row (`SELECT ... FOR UPDATE`), then loads the cassettes and contents and works on them. Two quotes for the same ATM are therefore serialized, and the second one sees the first one's reservation, so the same physical note can never be reserved twice. The lock is per ATM, so different ATMs do not block each other.

The daily limit has a second race that crosses ATMs: the same card used at two ATMs at once. A transaction-scoped advisory lock (`pg_advisory_xact_lock`) on the card hash serializes quotes of the same card. Locks are always taken in the same order (ATM, then card), so they cannot deadlock.

As a last line of defence the database enforces `NoteCount >= 0` and `0 <= ReservedCount <= NoteCount` with check constraints.

The integration tests send 30 parallel quotes for a cassette with 10 notes (exactly 10 succeed), pairs of simultaneous quotes for the last note, 20 parallel quotes of the same card against the daily limit, and 10 parallel confirms with one idempotency key. I also checked that these tests really guard the lock: with the `FOR UPDATE` removed, 30 quotes succeeded instead of 10 and all four tests failed.

### Two-step withdrawal and reservation (B4)

A quote runs the selector, stores up to three options (`EXACT`, or `LOWER` and `UPPER`) with their notes per cassette, and reserves the notes. If there are two options, the reservation per cassette is the larger of the two, so either one can be confirmed. On confirm, the chosen option is debited (`WITHDRAWAL` movements) and what was reserved but not used is released (`RESERVATION_RELEASE`). Reserved notes are subtracted from the available count, so no other quote can be offered them.

Expiry has two layers. Every quote, confirm and cancel first releases the expired quotes of that ATM, inside the ATM lock, so a stale reservation never blocks anything. A worker (`QuoteExpiryWorker`, every 5 seconds) also sweeps expired quotes so reservations are released without any traffic. Putting an ATM into maintenance cancels its pending quotes.

### Idempotency (B4)

`Idempotency-Key` is stored on the confirmed quote with a unique index. Under the ATM lock, a confirm with the key of an already confirmed quote returns the stored result without moving any cash; asking for a different option with the same key, or using a key for another quote, is a conflict (`IDEMPOTENCY_KEY_REUSED`); a different key on a confirmed quote is `QUOTE_NOT_CONFIRMABLE`. Because the lookup happens under the lock, parallel requests with the same key cannot both debit.

### Daily limit and card numbers (B5)

The limit counts confirmed amounts plus the amounts of pending quotes that have not expired, per card and per currency. The card number is never stored: the database keeps a keyed HMAC-SHA256 of it (key from configuration, validated at startup) for matching, plus the masked number (`************1111`) for display. A plain hash would be easy to brute-force because card numbers have little entropy; the key is what prevents that.

### Movement ledger and consistency (B8)

A cassette holds its notes in `CassetteContent` rows per currency and denomination, because the deposit cassette takes any note. Every change goes through one class, `CassetteMovementRecorder`, which updates the row and inserts an immutable `CassetteMovement` with two deltas: `NoteCountDelta` and `ReservedCountDelta`. `RESERVATION` changes only the reserved count, `WITHDRAWAL` changes both, and so on. Notes and reserved counts are therefore always the sum of the movements, and `GET /consistency` recomputes the sums per cassette, currency and denomination in a repeatable-read transaction and reports every difference. One integration test tampers with a stored balance and checks that it is detected.

### Reconciliation and four-eyes approval (B9)

Counts are entered per cassette while the ATM is in maintenance. The difference per row becomes a `RECONCILIATION_ADJUSTMENT` movement and is valued in TRY with the `t_ExchangeRates` table (seeded: 1 USD = 42 TRY, 1 EUR = 49 TRY). Above TRY 1,000 the reconciliation is `AWAITING_APPROVAL`; `put-in-service` is then rejected until a user with a different `X-User` approves it.

### Outbox and low-cash alerts (B10)

The movement recorder compares the available count of a recycling cassette before and after each movement. When it crosses below 10% of the capacity, it adds an `OutboxEvent` to the same `DbContext`, so the event is saved in the same transaction as the operation and disappears with it on rollback (there is a test that rolls back and checks this). `OutboxWorker` reads pending events in batches with `FOR UPDATE SKIP LOCKED` (several workers can run safely), logs them and marks them `Sent`.

### Error model

Errors are `application/problem+json` with the standard fields plus `errorCode` and `traceId`. Business errors are exceptions that carry a code and map to a status: not found 404, conflicting state 409, business rule violation 422, forbidden 403, bad request 400. Model validation errors are 400 with `VALIDATION_FAILED`; database unique and check violations are 409; anything unexpected is 500 `INTERNAL_ERROR` without internals, with the exception logged.

Codes: `VALIDATION_FAILED`, `INTERNAL_ERROR`, `UNIQUE_VIOLATION`, `CONSTRAINT_VIOLATION`, `ATM_NOT_FOUND`, `ATM_NAME_ALREADY_EXISTS`, `INVALID_ATM_STATE`, `INVALID_CASSETTE_CONFIGURATION`, `CASSETTE_NOT_FOUND`, `CASSETTE_NOT_EMPTY`, `CASSETTE_CAPACITY_EXCEEDED`, `CASSETTE_NOTE_MISMATCH`, `INSUFFICIENT_NOTES`, `NOTE_TYPE_REQUIRED`, `EMPTY_DEPOSIT`, `USER_HEADER_REQUIRED`, `RECONCILIATION_NOT_FOUND`, `RECONCILIATION_NOT_PENDING`, `RECONCILIATION_AWAITING_APPROVAL`, `FOUR_EYES_VIOLATION`, `INVALID_RECONCILIATION_COUNTS`, `EXCHANGE_RATE_MISSING`, `QUOTE_NOT_FOUND`, `QUOTE_EXPIRED`, `QUOTE_NOT_CONFIRMABLE`, `QUOTE_OPTION_REQUIRED`, `QUOTE_OPTION_NOT_OFFERED`, `DAILY_LIMIT_EXCEEDED`, `INSUFFICIENT_CASH`, `IDEMPOTENCY_KEY_REQUIRED`, `IDEMPOTENCY_KEY_REUSED`.

## Assumptions

Points the assignment leaves open, and what I decided:

1. **Foreign-currency cassettes.** "At least one and at most two of the recycling cassettes may be configured for a foreign currency" is read as a required range of one to two. A configuration with no USD or EUR cassette is rejected.
2. **When cash can be loaded.** Loading is allowed in `CONFIGURED`, `LOADED` and `MAINTENANCE` (the assignment names maintenance and initial setup, and I count `LOADED` as part of the initial setup). Unloading is allowed in `LOADED` and `MAINTENANCE`. Cassette configuration is allowed in `UNCONFIGURED`, `CONFIGURED` and `MAINTENANCE`. A cassette that still holds notes cannot be reconfigured.
3. **State changes caused by operations.** Configuring moves `UNCONFIGURED` to `CONFIGURED`, the first load moves `CONFIGURED` to `LOADED`; `IN_SERVICE` and `MAINTENANCE` are explicit calls. `IN_SERVICE` is only reachable from `LOADED` or `MAINTENANCE`.
4. **Amounts are whole numbers** of the currency (no coins), so a request such as 85 is valid and has no exact payout, but 85.5 is not.
5. **Limit day.** The daily limit uses the UTC calendar day.
6. **What counts towards the limit.** A pending quote counts with the largest amount it offered; after confirmation, with the confirmed amount. Cancelled and expired quotes count nothing. A quote whose amount is above the remaining limit is refused as a whole.
7. **Physical notes.** Reservations are counts, not individual notes: two quotes can get the same breakdown as long as enough notes remain available.
8. **Equal fill ratios and ties.** When several breakdowns need the same number of notes, the choice is deterministic but arbitrary. For two cassettes with the same denomination, dispensing drains the fuller one first and deposits fill the emptier one first.
9. **Deposits.** Notes of one request are placed in request order, which decides who gets the last free space of the deposit cassette. Unrecognised notes are only recorded as a count on the deposit transaction.
10. **Reconciliation value.** The TRY value of the differences is the sum of the absolute values, so a shortage in one cassette and a surplus in another do not cancel each other. Counts for the deposit cassette are entered per currency and denomination; a combination that the system holds but that is not listed is counted as zero. Every recycling cassette must be counted.
11. **Adjustments apply immediately.** The balances are corrected when the counts are entered; the approval only unlocks the return to service. A second reconciliation cannot be started while one awaits approval.
12. **Four-eyes identity.** Users are the `X-User` header values compared case-insensitively. There is no authentication.
13. **Exchange rates.** The seed rates (42 and 49) are placeholders so that the TRY calculation can run. There is no endpoint to maintain them.
14. **Idempotency scope.** An idempotency key is global, not per ATM, and is only used for confirming quotes.
15. **Low-cash alerts.** One event is raised when the available count crosses below 10% of the capacity (below 10 notes for a capacity of 100); it is raised again only after the cassette has been refilled above the threshold. Only recycling cassettes are watched.

## Unfinished items and known limitations

- **The Docker setup is untested in a container.** The machine I developed on has no Docker. I built the images' steps by hand (`dotnet restore` and `publish` exactly as in the `Dockerfile`) and ran the published API and worker in the `Production` environment with environment-variable configuration, but `docker compose up` itself has not been run. The first run may reveal small problems (for example the PostgreSQL 18 volume path or the container health probes).
- **Postman tests were not run in Postman.** I replayed the 27 requests in order against the API and checked every status code, but the JavaScript assertions inside the collection were not executed by Postman. They use values that the integration tests verify.
- **No authentication or authorization.** `X-User` is trusted as given. Swagger is open in every environment (set `OpenApi:Enabled=false` to hide it).
- **Ledger immutability is by construction, not enforced.** The code only ever inserts movements, but the database does not forbid updates or deletes (a trigger could).
- **Throughput.** The per-ATM lock serializes all operations on one ATM, which is the price of the strong guarantees. It is fine for one ATM, but a single hot ATM is a bottleneck.
- **Time zones.** The daily limit day is UTC; a real network would use the time zone of the ATM.
- **Outbox delivery is at-least-once** and the only consumer logs the event. If the worker dies between logging and marking, the event is logged again.
- **Stale reservations in reports.** The report can show the reservation of an expired quote as reserved for up to one sweep interval (5 seconds) until the worker releases it.
- **Missing features outside the assignment:** reject action for reconciliations, pagination of lists, editing or deleting an ATM, a rates endpoint, rate history (a reconciliation uses the current rates and does not store which ones).
- **Layering.** The Application project references Infrastructure and uses `AppDbContext` directly, as the Dashboard services do, which is looser than a strict "Application depends only on Domain" layering. A repository layer would fix it at the cost of a lot of code.

## AI tools

I built this with Claude Code (Claude Sonnet 5.5) in an interactive session, working step by step and committing after each step. I used it to study the Dashboard API of my earlier project and follow its structure, to design and write the code, the migrations, the tests, the Docker setup and the Postman collection, and to draft this document. I decided the scope, accepted or changed the design choices and the assumptions above, and ran and read the results.

Things that came out wrong the first time, and what was corrected:

- The entity `Atm` collided with the `Fimple.Atm` namespace and did not compile; it became `AtmMachine`.
- The worker crashed at runtime because it resolved an older EF Core Relational assembly (10.0.4); the package version is now pinned.
- Upgrading `Microsoft.OpenApi` directly to a new major version broke the build; the fix was to update `Microsoft.AspNetCore.OpenApi`, which brings a patched `Microsoft.OpenApi` 2.x.
- The Release build showed two nullable warnings that the Debug build did not; they are now explicit guards.
- A stale solution file that Visual Studio re-created had been committed by accident and was removed.
- A test helper had an untyped SQL parameter and failed against PostgreSQL; fixed.
- To make sure the concurrency tests prove something, the row lock was removed on purpose and the tests had to fail (they did) before the lock was restored.
- Some first guesses at the requirements were revisited, for example the deposit and reconciliation rules listed under Assumptions.
