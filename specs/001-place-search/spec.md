# Photon place search
Status: implementing

## Goal and actors
Registered users search addresses, named places and nearby pharmacies without a
provider account, paid subscription or imported local geographic database.

## Requirements
- FR-1: WHEN a registered user supplies a text query, the API SHALL return normalized place candidates.
- FR-2: WHEN category=pharmacy and a valid center are supplied, the API SHALL search nearby pharmacies.
- FR-3: IF input is invalid, the API SHALL return 400 with camelCase field errors before calling Photon.
- FR-4: WHEN a search completes, persisted plans, locations and revisions SHALL remain unchanged.
- FR-5: IF Photon is unavailable, busy, returns an error or malformed JSON, the API SHALL return a stable 503 code.
- FR-6: WHEN search requests exceed the local user limit, the API SHALL return 429.
- FR-7: WHEN a text search includes a bounding box, the search SHALL restrict results to that extent.

## Non-functional requirements
Use HTTPS and a replaceable provider port. Bound HTTP duration to 10 seconds and body size
 to 1 MiB. Permit one outbound request at a time per process, spaced at least one second
apart; reject overlapping requests instead of accumulating an unbounded queue. Do not log
search terms, coordinates, upstream URLs or response bodies. Propagate cancellation.

## Boundary cases
Exactly one of query/category is required. Query length: 3-200; limit: 1-10; pharmacy
radius: (0,10] km. Coordinates must be paired and within geographic ranges. Empty
results are 200 with []. Missing optional address fields are allowed. Bounding boxes
use west,south,east,north; antimeridian-spanning boxes are not supported in this version.

## Out of scope
Angular form/map implementation, map tiles, routing, geolocation permission, database
migrations, provider accounts and additional categories. Keep existing atomic recado
creation: the client sends the selected result in newLocation.

## Open decisions
No blocking decisions. Result names use the provider default/local language; UI strings
remain independently translatable. Public Photon has no uptime or numeric quota guarantee.

## Completion criteria
Source changes and manual HTTP examples delivered; validation records actual evidence
and distinguishes unexecuted .NET/API checks. Runtime verification is required before release.
