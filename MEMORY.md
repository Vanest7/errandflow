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

Verified in this correction pass: source/diff review, git diff --check and
`dotnet build backend/src/ErrandFlow.Api/ErrandFlow.Api.csproj --no-restore` with zero
warnings/errors. `dotnet test` completes successfully but no backend test projects exist,
so this is not evidence of application test coverage.
NOT RUN: HTTP execution, SQL execution and live schema comparison.

Pending: compare the existing database with the baseline before any incremental migration;
verify runtime behavior and add tests in the final testing phase. Deployment-specific JWT
Authority/Audience and connection secrets remain external configuration.
Next feature: add focused domain/application tests and validate the MariaDB schema against
the target environment.
