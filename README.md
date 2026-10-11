# ATM Cash Management Backend

## What this project does

A bank runs many ATMs and needs to know how much cash each machine holds, what it can pay out, and whether the cash in the machine matches the books. This project is the backend for that. It is a REST API built with ASP.NET Core on .NET 10, EF Core and PostgreSQL. There is no user interface.

The system does the following.

- Keeps a network of ATMs. Each ATM has five cassettes that hold banknotes. One cassette only takes deposits and four cassettes recycle cash, each for one currency and one denomination.
- Pays withdrawals in two steps. The customer asks for an amount, the system offers the exact payout or the closest alternatives using the fewest notes, and the notes stay reserved for 60 seconds until the customer confirms or cancels.
- Accepts deposits and puts every note into the right cassette, or hands it back when there is no room.
- Records every change to a cassette as an immutable movement, so any balance can be rebuilt and checked.
- Reconciles the counted cash against the system, and asks a second person to approve large differences.
- Reports the notes in each ATM and raises a low-cash alert when a cassette runs short.

Supported currencies are TRY, USD and EUR. Supported denominations are 20, 50, 100 and 500.

## How an ATM moves through the API

This is the normal order of calls. The same order is in the Postman collection.

1. **Create the ATM** with `POST /api/atms`. It starts as `UNCONFIGURED`.
2. **Configure the cassettes** with `PUT /api/atms/{id}/cassettes`. One cassette is the deposit cassette. Four are recycling cassettes and one or two of them must hold USD or EUR. The ATM becomes `CONFIGURED`.
3. **Load cash** with `POST /api/atms/{id}/load`. This records the notes that were put in by hand. The ATM becomes `LOADED`.
4. **Put it in service** with `POST /api/atms/{id}/put-in-service`. The ATM becomes `IN_SERVICE`. Withdrawals and deposits only work in this state.
5. **Request a quote** with `POST /api/atms/{id}/withdrawals/quotes`. The response is the exact breakdown, or a lower and an upper alternative when the amount cannot be paid exactly. The offered notes are reserved for 60 seconds and the amount counts against the card's daily limit.
6. **Confirm or cancel the quote.** Confirm with `POST .../quotes/{quoteId}/confirm` and an `Idempotency-Key` header. Sending the same key again returns the same result and never pays twice. Cancel with `POST .../quotes/{quoteId}/cancel` to release the reservation. An expired or cancelled quote cannot be confirmed.
7. **Deposit notes** with `POST /api/atms/{id}/deposits`. The response shows which cassette took each note and lists the notes that were returned.
8. **Switch to maintenance** with `POST /api/atms/{id}/enter-maintenance`. Pending quotes are cancelled. Cash can now be loaded, unloaded and counted.
9. **Reconcile** with `POST /api/atms/{id}/reconciliations`. The counted notes are sent in and the system records every difference as an adjustment. When the total difference is worth more than TRY 1,000 the reconciliation waits for approval. A different user must call `POST .../reconciliations/{rid}/approve` with their own `X-User` header before the ATM can go back into service.
10. **Check and report.** `GET /api/atms/{id}/consistency` compares the stored balances with the balances rebuilt from the movements. `GET /api/reports/operations` shows note, reserved and available counts per cassette, totals per currency and the overall total in TRY.

At any time `GET /api/atms/{id}` shows the notes of each denomination in an ATM and its total balance.

### All endpoints

| Method and path | Purpose |
|---|---|
| `GET /api/atms` and `GET /api/atms/{id}` | List ATMs, or show one ATM with cassettes, notes per denomination and totals per currency |
| `POST /api/atms` | Create an ATM |
| `PUT /api/atms/{id}/cassettes` | Configure the five cassettes |
| `POST /api/atms/{id}/load` and `/unload` | Record cash loaded or removed by hand |
| `POST /api/atms/{id}/put-in-service` and `/enter-maintenance` | State transitions |
| `POST /api/atms/{id}/withdrawals/quotes` | Request a quote and reserve the notes |
| `GET /api/atms/{id}/withdrawals/quotes/{quoteId}` | Read a quote |
| `POST /api/atms/{id}/withdrawals/quotes/{quoteId}/confirm` | Confirm a quote, needs the `Idempotency-Key` header |
| `POST /api/atms/{id}/withdrawals/quotes/{quoteId}/cancel` | Cancel a quote |
| `POST /api/atms/{id}/deposits` | Deposit notes |
| `GET` and `POST /api/atms/{id}/reconciliations` | List or start reconciliations, needs the `X-User` header |
| `GET /api/atms/{id}/reconciliations/{rid}` | Read one reconciliation |
| `POST /api/atms/{id}/reconciliations/{rid}/approve` | Approve a reconciliation, needs the `X-User` header |
| `GET /api/atms/{id}/consistency` | Ledger consistency check |
| `GET /api/reports/operations` | Operations report, optional `?atmId=` |

Enums are upper snake case in JSON, for example `TRY`, `IN_SERVICE` and `RECONCILIATION_ADJUSTMENT`. Denominations are plain numbers.

## How to run the project and its tests

### With Docker

```bash
docker compose up --build
```

| What | Where |
|---|---|
| Swagger UI | http://localhost:8080/swagger and the root URL redirects there |
| OpenAPI document | http://localhost:8080/openapi/v1.json |
| Health, readiness | http://localhost:8080/health |
| Health, liveness | http://localhost:8080/health/live |
| PostgreSQL from the host | `localhost:5433`, database `fimple_atm`, user `postgres` |

The API applies the migrations on startup, so nothing has to be prepared. The worker starts after the API is healthy. The database password and the card hashing key have local defaults in `docker-compose.yml`. Copy `.env.example` to `.env` to change them. Use only letters, digits, dashes and underscores in the password because it goes into a connection string.

### Without Docker

You need the .NET 10 SDK and a PostgreSQL server. Put your database password into `appsettings.Local.json` in the repository root. The file is git-ignored. Create it from the example and replace the password.

```bash
cp appsettings.Local.example.json appsettings.Local.json
```

In the Development environment the API, the worker and the tests read this file automatically. It overrides `appsettings.Development.json`, which only holds a `CHANGE_ME` placeholder. An environment variable such as `ConnectionStrings__PostgreSql` still wins over both.

```bash
dotnet run --project Fimple.Atm.Api --launch-profile http
dotnet run --project Fimple.Atm.Worker
```

The API is then at http://localhost:5272/swagger. On startup it applies the migrations and creates the database when it does not exist, so the database user needs the right to create databases. Outside Development the setting `CardHashing__Key` must be provided with at least 32 characters, otherwise the application refuses to start.

### Tests

```bash
dotnet test
```

There are 55 tests. 21 are unit tests for the note selector, including the eight acceptance cases of requirement B3, and for the deposit allocator. 34 are integration tests that run against a real PostgreSQL database.

The integration tests create and drop their own database named `fimple_atm_test`. They start the API in memory with `WebApplicationFactory` and a fake clock and call it over HTTP. The connection string comes from the `FIMPLE_TEST_CONNECTION_STRING` environment variable or from `appsettings.Local.json`. For example, against the compose database.

```bash
docker compose up -d postgres
FIMPLE_TEST_CONNECTION_STRING="Host=localhost;Port=5433;Username=postgres;Password=fimple-local-dev-password" dotnet test
```

### Postman

Import `postman/ATM_Cash_Management.postman_collection.json` and one environment. Use `ATM_docker_compose` for port 8080 or `ATM_dotnet_run` for port 5272. Run the collection in order with the Collection Runner. Folder 1 is the scenario from the assignment. Folder 2 covers the four-eyes approval. Folder 3 covers alternatives, cancellation and error cases. Ids are passed between requests through collection variables.

## Architecture and key design decisions

### Structure

```
Fimple.Atm.Api            controllers, exception middleware, OpenAPI, health checks
Fimple.Atm.Application    services, note selector, deposit allocator, movement recorder
Fimple.Atm.Domain         entities, enums, rule constants, models, interfaces, exceptions
Fimple.Atm.Infrastructure AppDbContext, migrations, repositories, unit of work
Fimple.Atm.Worker         quote expiry worker and outbox worker
Fimple.Atm.Tests          unit and integration tests
```

Dependencies point inward. Domain depends on nothing. Application and Infrastructure depend only on Domain. The API and the worker wire everything together. Services reach the database through repository interfaces and a unit of work that are declared in Domain and implemented in Infrastructure, so the Application layer has no EF Core code.

The layout follows the Dashboard API from my earlier project, with request and return models per action. Where the assignment asks for something else, the assignment wins. That is why this project uses ProblemDetails, REST routes, an `X-User` header instead of JWT, tests against a real database and Swagger.

### Note selection

Starting from the largest denomination gives wrong answers for some of the B3 cases. The selector is instead a bounded dynamic program over the notes in stock. Cassettes with the same denomination are merged into one stock. Amounts are divided by the greatest common divisor of the denominations. The table `minimumNotes[amount]` is filled denomination by denomination and never uses more notes than are in stock. The exact answer with the fewest notes comes straight from the table. When the amount cannot be paid, the table is scanned downwards and upwards to find the nearest payable amounts. These are the lower and upper alternatives. The service drops the upper alternative when it would go over the card's remaining daily limit.

Once the count per denomination is known, notes of one denomination are taken from the cassette with the higher fill ratio first. The selector is a pure function, which is why it is easy to test.

### Concurrency

Every operation that changes an ATM runs in one database transaction that first locks the ATM row with `SELECT ... FOR UPDATE`. Two quotes for the same ATM are processed one after the other, so the second one sees the reservation of the first and the same note is never reserved twice. Different ATMs do not block each other.

The same card can be used at two ATMs at the same moment, which would bypass the daily limit. A transaction level advisory lock on the card hash prevents this. Locks are always taken in the same order, first the ATM and then the card, so they cannot deadlock.

The database also enforces `NoteCount >= 0` and `0 <= ReservedCount <= NoteCount` with check constraints.

The integration tests send 30 parallel quotes against a cassette with 10 notes and exactly 10 succeed. They also send pairs of quotes for the last note, 20 parallel quotes of one card against the daily limit, and 10 parallel confirms with one idempotency key. To be sure these tests mean something, I removed the lock on purpose. 30 quotes succeeded instead of 10 and all four tests failed.

### Two step withdrawal and reservation

A quote runs the selector, stores up to two options, and reserves the notes. The options are either one `EXACT` option, or a `LOWER` and an `UPPER` option. When there are two options, the reservation of each cassette is the larger of the two, so either option can be confirmed. On confirm the chosen option is debited with `WITHDRAWAL` movements and the rest of the reservation is released with `RESERVATION_RELEASE` movements. Reserved notes are subtracted from the available count, so no other quote can get them.

Expiry works in two ways. Every quote, confirm and cancel first releases the expired quotes of that ATM inside the ATM lock, so an old reservation never blocks anything. The `QuoteExpiryWorker` also sweeps expired quotes every 5 seconds, so reservations are released even when no request arrives.

### Idempotency

The `Idempotency-Key` is stored on the confirmed quote and has a unique index. Under the ATM lock, a confirm with the key of an already confirmed quote returns the stored result and moves no cash. Using the same key for another quote or for a different option is a conflict with the code `IDEMPOTENCY_KEY_REUSED`. A new key on an already confirmed quote returns `QUOTE_NOT_CONFIRMABLE`. Because the check happens under the lock, parallel requests with the same key cannot both debit.

### Daily limit and card numbers

The limit counts confirmed amounts and pending quotes that have not expired, per card and per currency. The card number is never stored. The database keeps a keyed HMAC-SHA256 hash for matching and a masked number such as `************1111` for display. A plain hash would be easy to brute force because card numbers have little variety. The secret key prevents that.

### Movement ledger and consistency

A cassette keeps its notes in `CassetteContent` rows per currency and denomination, because the deposit cassette accepts any note. Every change goes through one class, `CassetteMovementRecorder`. It updates the row and inserts an immutable `CassetteMovement` with two deltas, `NoteCountDelta` and `ReservedCountDelta`. A `RESERVATION` changes only the reserved count and a `WITHDRAWAL` changes both. Because of this, the stored counts always equal the sum of the movements. The consistency endpoint recomputes the sums in a repeatable read transaction and reports every difference. One integration test changes a stored balance by hand and checks that it is detected.

### Reconciliation and four eyes approval

Counts are entered while the ATM is in maintenance. The difference of each row becomes a `RECONCILIATION_ADJUSTMENT` movement and is valued in TRY with the `t_ExchangeRates` table. The seed rates are 1 USD = 42 TRY and 1 EUR = 49 TRY. Above TRY 1,000 the reconciliation becomes `AWAITING_APPROVAL` and `put-in-service` is rejected until a user with a different `X-User` approves it.

### Outbox and low-cash alerts

The movement recorder compares the available count of a recycling cassette before and after each movement. When it drops below 10 percent of the capacity, it adds an `OutboxEvent` to the same `DbContext`. The event is saved in the same transaction as the operation and disappears with it on rollback, and a test checks this. The `OutboxWorker` reads pending events in batches with `FOR UPDATE SKIP LOCKED`, so several workers can run safely. It logs each event and marks it as sent.

### Error model

Errors are returned as `application/problem+json` with the standard fields plus `errorCode` and `traceId`. Business errors carry a code and map to a status. Not found is 404, a conflicting state is 409, a broken business rule is 422, forbidden is 403 and a bad request is 400. Model validation errors are 400 with `VALIDATION_FAILED`. Database unique and check violations are 409. Anything unexpected is a 500 with `INTERNAL_ERROR`, without internal details, and the exception is logged.

The codes are `VALIDATION_FAILED`, `INTERNAL_ERROR`, `UNIQUE_VIOLATION`, `CONSTRAINT_VIOLATION`, `ATM_NOT_FOUND`, `ATM_NAME_ALREADY_EXISTS`, `INVALID_ATM_STATE`, `INVALID_CASSETTE_CONFIGURATION`, `CASSETTE_NOT_FOUND`, `CASSETTE_NOT_EMPTY`, `CASSETTE_CAPACITY_EXCEEDED`, `CASSETTE_NOTE_MISMATCH`, `INSUFFICIENT_NOTES`, `NOTE_TYPE_REQUIRED`, `EMPTY_DEPOSIT`, `USER_HEADER_REQUIRED`, `RECONCILIATION_NOT_FOUND`, `RECONCILIATION_NOT_PENDING`, `RECONCILIATION_AWAITING_APPROVAL`, `FOUR_EYES_VIOLATION`, `INVALID_RECONCILIATION_COUNTS`, `EXCHANGE_RATE_MISSING`, `QUOTE_NOT_FOUND`, `QUOTE_EXPIRED`, `QUOTE_NOT_CONFIRMABLE`, `QUOTE_OPTION_REQUIRED`, `QUOTE_OPTION_NOT_OFFERED`, `DAILY_LIMIT_EXCEEDED`, `INSUFFICIENT_CASH`, `IDEMPOTENCY_KEY_REQUIRED` and `IDEMPOTENCY_KEY_REUSED`.

## Assumptions

These are the points the assignment leaves open and what I decided.

1. **Foreign currency cassettes.** The rule says at least one and at most two recycling cassettes may hold a foreign currency. I read it as a required range of one to two. A configuration without a USD or EUR cassette is rejected.
2. **When cash can be loaded.** Loading is allowed in `CONFIGURED`, `LOADED` and `MAINTENANCE`. I count `LOADED` as part of the initial setup. Unloading is allowed in `LOADED` and `MAINTENANCE`. Configuring the cassettes is allowed in `UNCONFIGURED`, `CONFIGURED` and `MAINTENANCE`. A cassette that still holds notes cannot be reconfigured.
3. **State changes caused by operations.** Configuring moves `UNCONFIGURED` to `CONFIGURED` and the first load moves `CONFIGURED` to `LOADED`. `IN_SERVICE` and `MAINTENANCE` are explicit calls. `IN_SERVICE` can only be reached from `LOADED` or `MAINTENANCE`.
4. **Whole amounts.** Amounts are whole numbers because coins are not supported. A request for 85 is valid and has no exact payout, but 85.5 is rejected.
5. **Limit day.** The daily limit uses the UTC calendar day.
6. **What counts towards the limit.** A pending quote counts with the largest amount it offered. After confirmation it counts with the confirmed amount. Cancelled and expired quotes count nothing. A quote above the remaining limit is refused as a whole.
7. **Reserved notes.** Reservations are counts and not individual notes. Two quotes can get the same breakdown as long as enough notes remain available.
8. **Ties.** When several breakdowns use the same number of notes, the choice is deterministic but arbitrary. With two cassettes of the same denomination, a withdrawal empties the fuller one first and a deposit fills the emptier one first.
9. **Deposits.** Notes of one request are placed in request order. This decides who gets the last free space of the deposit cassette. Unrecognised notes are only stored as a count on the deposit.
10. **Reconciliation value.** The TRY value of the differences is the sum of the absolute values, so a shortage in one cassette and a surplus in another do not cancel each other. Counts for the deposit cassette are entered per currency and denomination. A combination that the system holds but that is not listed counts as zero. Every recycling cassette must be counted.
11. **Adjustments apply immediately.** Balances are corrected when the counts are entered. The approval only unlocks the return to service. A second reconciliation cannot be started while one waits for approval.
12. **Four eyes identity.** Users are the `X-User` header values, compared without case. There is no authentication.
13. **Exchange rates.** The seed rates 42 and 49 are placeholders so that the TRY calculation can run. There is no endpoint to maintain them.
14. **Idempotency scope.** A key is global and not per ATM. It is only used for confirming quotes.
15. **Low-cash alerts.** One event is raised when the available count drops below 10 percent of the capacity, which means below 10 notes for a capacity of 100. It is raised again only after the cassette has been refilled above the threshold. Only recycling cassettes are watched.

## Unfinished items and known limitations

- **The Docker setup was not run in a container.** My machine has no Docker. I ran the same `dotnet restore` and `publish` steps as the `Dockerfile` by hand and started the published API and worker in the `Production` environment with environment variables. `docker compose up` itself has not been run, so the first run may show small problems such as the PostgreSQL 18 volume path or the container health probes.
- **Postman scripts were not run in Postman.** I replayed the 27 requests in order against the API and checked every status code. The JavaScript assertions inside the collection were not executed by Postman. They use values that the integration tests verify.
- **No authentication or authorization.** The `X-User` header is trusted as given. Swagger is open in every environment and can be hidden with `OpenApi:Enabled=false`.
- **Ledger immutability is not enforced by the database.** The code only inserts movements, but the database would allow updates and deletes. A trigger could forbid them.
- **Throughput.** The per ATM lock serializes all operations on one ATM. This is fine for one machine, but a single very busy ATM is a bottleneck.
- **Time zones.** The limit day is UTC. A real network would use the time zone of the ATM.
- **Outbox delivery is at least once.** The only consumer logs the event. If the worker stops between logging and marking, the event is logged again.
- **Stale reservations in reports.** A report can show the reservation of an expired quote for up to 5 seconds, until the worker releases it.
- **Features outside the assignment.** There is no reject action for reconciliations, no pagination, no editing or deleting of an ATM, no endpoint for exchange rates and no rate history. A reconciliation uses the current rates and does not store which ones.

## AI tools

I built this with Claude Code, using Claude Sonnet 5.5 in an interactive session, step by step with a commit after each step. I used it to study the Dashboard API of my earlier project and follow its structure, to design and write the code, the migrations, the tests, the Docker setup and the Postman collection, and to draft this document. Claude Code worked under the pinq-doq coding standards, the shared rule set of my team. It covers naming, layering, error handling, constants and comments, and the code and the review fixes follow it. I decided the scope, accepted or changed the design choices and the assumptions above, and ran and read the results.

These things came out wrong the first time and were corrected.

- The entity `Atm` collided with the `Fimple.Atm` namespace and did not compile. It became `AtmMachine`.
- The worker crashed at runtime because it loaded an older EF Core Relational assembly. The package version is now pinned.
- Upgrading `Microsoft.OpenApi` directly to a new major version broke the build. The fix was to update `Microsoft.AspNetCore.OpenApi`, which brings a patched `Microsoft.OpenApi` 2.x.
- The Release build showed two nullable warnings that the Debug build did not. They are now explicit guards.
- A stale solution file that Visual Studio re-created was committed by accident and then removed.
- A test helper had an untyped SQL parameter and failed against PostgreSQL. It was fixed.
- To prove the concurrency tests work, the row lock was removed on purpose. The tests failed as they should and the lock was restored.
- Some first guesses about the requirements were revisited, for example the deposit and reconciliation rules in the assumptions.
