# Implementation plan

- Reuse `ErrandForm` for create/edit. Edit mode preloads title and duration; unchanged location uses `locationId`, changed location uses `newLocation`.
- Expand `ErrandsApiService` with generated OpenAPI update/delete/reorder calls.
- Orchestrate state, optimistic concurrency, user confirmations and error recovery in `PlanEditor`.
- Use deterministic `moveErrandIds` pure helper for the original order; add tests for boundaries.
- Preserve map and itinerary integration, invalidating a calculated route on mutation attempts.
- Extend ES/EN translations in `frontend/public/i18n`.
