# 004 Errand schedule management

## Goal
Allow users to set and clear time constraints for existing errands, in the plan's time zone, without modifying the route calculation or generated API layer.

## Requirements
- SCHEDULE-01: Each existing errand offers an accessible schedule control, with three choices: unrestricted, earliest-start/latest-finish window, or fixed appointment start.
- SCHEDULE-02: The interface shows saved constraints in the plan's time zone and supports ES/EN translations.
- SCHEDULE-03: The client converts local wall time to UTC only when that time has a unique instant in the plan's time zone. It rejects DST gaps and overlaps.
- SCHEDULE-04: Windows require a valid start, a later finish, and enough time for the errand duration; crossing midnight is supported.
- SCHEDULE-05: Mutations send expectedRevision to the existing schedule endpoint. On success, the route is invalidated and the local errand and revision are updated. Errors remain visible without discarding the form.
- SCHEDULE-06: Scheduling must not overlap with edit/delete/reorder or endpoint changes; no new dependencies are required.

## Explicit limitation
These three UI modes are mutually exclusive. Choosing and saving one mode replaces any previously stored schedule constraints. The backend supports combining fixed and window fields, but that advanced combination is not exposed in this MVP form.

## Verification
Run focused UTC/DST tests, frontend build, lint, test suite, and a browser smoke with JWT/MariaDB/Valhalla.
