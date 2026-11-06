# Backend corrections

## Plans

POST /api/plans returns 201 with a Location generated from the named GetPlanById route.
GET /api/plans/{id} returns Revision; use that revision for subsequent changes.
DELETE /api/plans/{id}?expectedRevision=1 requires a positive revision observed by the client.
Missing, malformed or zero revisions return 400. A missing or other-owner plan returns 404.
A stale revision returns 409, including a concurrent update/delete detected during SaveChanges.
A successful deletion returns 204. PUT retains its existing ExpectedRevision body contract.
Clients must refresh the plan after a conflict rather than retry with an invented revision.

PUT /api/plans/{id}/locations accepts ExpectedRevision plus nullable OriginLocationId and
DestinationLocationId. Non-null locations must belong to the authenticated user's plan;
the response is the updated plan and its incremented Revision. GET /api/plans/{id} exposes
both selected location IDs.

GET /api/plans/{id}/locations returns the owner-scoped locations ordered by label and ID.
An unknown or other-owner plan returns 404.
PUT /api/plans/{planId}/locations/{locationId} accepts the editable label, address,
coordinates and ExpectedRevision. It returns the updated location and plan revision.
DELETE /api/plans/{planId}/locations/{locationId}?expectedRevision=1 returns 204 only
when the location is not an errand location or a plan origin/destination; otherwise it
returns 409 with code location_in_use.

PUT /api/plans/{planId}/errands/order accepts ExpectedRevision and ErrandIds. The list
must contain every errand in the plan exactly once. It returns the reordered errands and
the incremented plan revision. Position updates use a transaction and reserve a range
above the current maximum position before assigning final positions, preserving the
unique per-plan position constraint even after deletions. If the plan disappears during
the operation, the endpoint returns 404; a stale revision remains a 409 conflict.

Updating an errand with a new location creates that location but does not delete the
previous location. Locations are removed only through the explicit location-delete
endpoint.

PUT /api/plans/{planId}/errands/{errandId}/schedule accepts ExpectedRevision and nullable
EarliestStartAt, LatestFinishAt and FixedStartAt. Schedule timestamps must be UTC; the
domain validates interval, fixed-start and duration feasibility rules. The response includes
the updated errand and plan revision.

## Errors

ProblemDetails includes a stable `code` and request `traceId`. Validation responses also include
an `errors` dictionary keyed by field. Human-readable messages are fallback English strings;
clients should translate codes and field labels rather than match message text.
Codes: validation_failed, plan_revision_conflict, authentication_required, access_denied,
resource_not_found, method_not_allowed, unsupported_media_type, internal_error, request_failed.
Framework authentication failures and empty HTTP errors are covered by status-code pages.
Exception internals are not included in client responses.

Infrastructure translates EF concurrency failures into Application's PlanConcurrencyException.
The HTTP layer has no dependency on EF exception types. Plan ownership uses a required User
foreign key with restricted deletion. Database rollout follows backend/database/README.md.

Tests are explicitly deferred by the user. Build, live HTTP and database verification remain
pending where the corresponding runtime/services are unavailable.

## Review corrections (2026-11-05)

- Schedule updates check that the owner-scoped plan contains the errand before comparing
  revisions, consistently returning 404 for a missing errand even with a stale revision.
- Schedule and plan-location input errors use camelCase JSON field names.
- The empty-database baseline and EF mappings preserve composite origin/destination
  foreign keys and plan/location/errand CHECK constraints. Existing databases require
  inspection; this correction does not execute a migration.

Verification for this correction: diff review, `git diff --check`, and a static comparison
of baseline CHECK names/expressions against EF mappings passed. Build, automated .NET
tests, HTTP requests and MariaDB execution were NOT RUN: the review environment has no
.NET SDK or configured application/database services. Tests remain deferred as agreed.

## Place search

GET /api/places/search supports text query or category=pharmacy. See docs/place-search.md
for parameter validation, bounding boxes and local limits. Search is read-only.
Errors include rate_limit_exceeded (429) and place_search_unavailable (503); clients
must distinguish provider failure from a successful empty result.
