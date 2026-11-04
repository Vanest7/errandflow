# Project handoff
Updated: 2026-11-04
Phase: backend Plans and Auth0 hardening.
Active spec: none.
Completed: Plans ownership-scoped query and CRUD use cases; JWT Bearer registration; centralized ProblemDetails errors; optimistic concurrency; UTC-owned audit timestamps; generated-file ignore rules.
Verified: `dotnet build backend/ErrandFlow.slnx` passes with 0 warnings and 0 errors; `dotnet test backend/ErrandFlow.slnx --no-restore` passes; `git diff --check` passes; no generated `bin/` or `obj/` files remain tracked.
Completed: Auth0 registration policy with the database lookup moved out of JWT validation.
Pending: configure deployment-specific JWT Authority/Audience, automated application tests and database migration workflow.
Next: add focused authentication/application tests and define the first migration before persistence rollout.
Do not mark unrun tests or migrations as completed.
