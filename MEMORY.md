# Project handoff
Updated: 2026-11-05
Phase: backend Plans, Locations and Errands contracts.
Active spec: none; focused corrections documented in docs/backend-contracts.md.

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
