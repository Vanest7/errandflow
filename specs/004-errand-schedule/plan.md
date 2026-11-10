# Implementation plan

- Extend `features/plans/data-access/errands-api.service.ts` using generated `SetErrandScheduleInput` and the existing PUT schedule request.
- Keep wall-time parsing, validation, conversion and formatting in `features/plans/utils/errand-schedule-time.ts`.
- Add a focused `ErrandScheduleForm` under `features/plans/components`; its output is a typed schedule payload, without HTTP dependencies.
- Integrate a schedule action, constraint summary, and editing state in `PlanEditor`, preserving local edits to the itinerary and drag-and-drop.
- Add ES/EN dictionaries under `frontend/public/i18n` and focused utility tests.
- Install with a guarded, idempotent patch script that checks all paths before writing.
