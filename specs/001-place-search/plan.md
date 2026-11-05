# Implementation plan
## Requirement-to-component mapping
FR-1/2/7: SearchPlaces, IPlaceSearchProvider, PhotonPlaceSearchProvider, PlacesController.
FR-3: Application input validation plus standard MVC model binding errors.
FR-4: Search has no persistence dependency; existing recado write contract is unchanged.
FR-5: PlaceSearchUnavailableException and centralized ProblemDetails mapping.
FR-6: authenticated user fixed-window limiter and shared provider gate.
## Files and responsibilities
Application owns neutral DTOs/port/use case; Infrastructure owns Photon GeoJSON models,
HTTP mapping and provider gate; Api owns DI, authentication and HTTP status codes.
## Data/API contracts and failure behavior
See docs/place-search.md. External IDs are never treated as Location IDs.
## Decisions and alternatives
Use the public Photon HTTPS endpoint; no API key. Keep legacy documented osm_tag filter
for compatibility with the tested public server. Allow configured HTTPS base URL.
The per-process guard is for a single-instance MVP, not a distributed quota guarantee.
## Verification strategy
Review boundaries, run diff checks and smoke-test the real public provider. Build if the
SDK is available. Automated project tests remain deferred per the existing agreement;
record unrun API/error/cancellation checks explicitly.
## Dependencies and migration impact
No added NuGet dependencies or database migrations. ASP.NET supplies HttpClient factory
and rate-limiting support. Follow OSM attribution and Photon fair-use conditions.
