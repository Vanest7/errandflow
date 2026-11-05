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
