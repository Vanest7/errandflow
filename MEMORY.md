# Project handoff
Updated: 2026-11-05
Phase: backend Plans, Locations and Errands contracts.
Active spec: specs/001-place-search/spec.md; prior corrections in docs/backend-contracts.md.

Implemented: ownership-scoped Plans CRUD, JWT registration policy, named-route creation
responses, revision-aware deletion, Infrastructure translation of EF concurrency errors,
stable ProblemDetails codes and field validation errors, explicit Plan-to-User foreign key.
Added: versioned empty-database SQL baseline and read-only existing-schema inspection.
Added: errand schedule mutation, plan origin/destination selection with ownership checks,
owner-scoped plan location listing, and updated Plan DTO location fields.

Previously recorded verification (before the review corrections below): source/diff review, git diff --check and
`dotnet build backend/src/ErrandFlow.Api/ErrandFlow.Api.csproj --no-restore` with zero
warnings/errors. `dotnet test` completes successfully but no backend test projects exist,
so this is not evidence of application test coverage.
NOT RUN: HTTP execution, SQL execution and live schema comparison.

Pending: compare the existing database with the baseline before any incremental migration;
verify runtime behavior and add tests in the final testing phase. Deployment-specific JWT
Authority/Audience and connection secrets remain external configuration.
Next feature: add focused domain/application tests and validate the MariaDB schema against
the target environment.

Review corrections (2026-11-05): aligned schedule missing-resource precedence with other
errand mutations; standardized the new use-case validation keys to camelCase; restored
composite origin/destination foreign keys and plan/location/errand CHECK constraints in
the empty-database baseline and EF mappings. No live schema changes were executed.
Verified: source/diff review, git diff --check, and static SQL/EF CHECK parity.
NOT RUN for these corrections: build and .NET tests (SDK unavailable), HTTP execution,
MariaDB constraint execution and live schema comparison. Prior build results do not
validate these changes. Next: build locally and verify missing-errand/stale-revision,
field-error keys, and origin/destination integrity on a disposable database.

Photon backend search added: GET /api/places/search, registered-user authorization,
text/name search with optional center/bounding box and nearby pharmacy search, neutral
DTOs, configurable HTTPS Photon adapter, timeouts, bounded response size, per-user rate
limits and per-process outbound gate. Search does not persist locations or mutate plans.
No API key, provider account, new NuGet package or database migration is required.
Verified with temporary SDK 10.0.403: backend build (0 warnings/errors), 20 isolated
source-linked checks and git diff --check. No permanent test project was added.
NOT RUN: authenticated API/model-binding/rate-limit checks with the user's JWT/database.
Next: apply changes, run examples in Places.http, then connect Angular search and map
selection; preserve OSM attribution. Details: docs/place-search.md and active validation.md.
Live Photon adapter smoke also passed: text-with-bounds and nearby pharmacies each
returned five normalized results. This does not replace authenticated API verification.

Unified place search extension: exact Spanish/English category aliases now select one of
18 nearby establishment types. Matching ignores accents, case and repeated whitespace;
other queries remain name/address searches. Categories require center/radius and reject
boundingBox. Existing category=pharmacy remains supported. Application owns neutral
aliases; Infrastructure owns OSM tags. No database writes or new packages.
Verified: backend build with SDK 10.0.403 (0 warnings/errors), 61 isolated source-linked
checks, git diff --check, and live bakery/supermarket/named-store adapter smoke (five
results each after an earlier timeout). Authenticated API checks remain pending.
Next: apply the incremental change and run the new examples in Places.http with a token;
then connect Angular search/map selection. Provider coverage is not exhaustive.

Location and errand ordering correction (2026-11-06): added revision-aware location
update/delete endpoints, ownership-scoped tracking queries, location-in-use conflict
mapping, cleanup of an old unreferenced location when an errand receives a new one,
and PUT /api/plans/{planId}/errands/order with exact-ID validation and transactional
position updates. Build and the existing 62 backend tests pass after the change.
Not run: live JWT/API checks, MariaDB execution, and real two-user authorization checks.
The new contracts are documented in docs/backend-contracts.md.

Follow-up correction (2026-11-06): errand reordering now reserves consecutive temporary
positions above the current maximum, checks UInt32 capacity, and returns an explicit
not-found result when the plan disappears during the transaction; revision conflicts
remain PlanConcurrencyException/409. Updating an errand with a new location no longer
deletes the previous location; deletion remains explicit through the location endpoint.
Verified with API build (0 warnings/errors), 62 existing backend tests, and git diff
--check. MariaDB execution and live HTTP/concurrency checks remain not run.
