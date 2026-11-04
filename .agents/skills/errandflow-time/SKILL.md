---
name: errandflow-time
description: Handle ErrandFlow dates, appointment times, durations, time windows,
  serialization and daylight-saving boundaries.
---

# Time semantics workflow
1. Distinguish calendar date, local wall time, time zone, absolute instant and duration.
2. Specify the plan's time zone. Do not derive business time from the server's local zone.
3. Preserve date-only values as date-only values; do not parse them into UTC instants accidentally.
4. At boundaries, convert local date/time plus zone to an instant using the agreed DST policy.
   Detect nonexistent and ambiguous wall times; never silently choose an offset.
5. Use explicit units for durations. An elapsed duration is not a calendar-day increment.
6. Define whether a window restricts service start or completion; test exact boundary equality.
7. Define fixed-appointment lateness behavior and cross-midnight handling in the active spec.
8. Test client/server zone differences, midnight, spring gaps, autumn overlaps and serialization.
9. Format for locale in Angular while preserving canonical values; keep translation and zone distinct.
Output: representations, conversions, unresolved policy and boundary-test evidence.
