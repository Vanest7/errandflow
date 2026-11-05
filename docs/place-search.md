# Place search

GET /api/places/search is protected by RegisteredUser. Use the same Bearer token as
plans. No Photon account, API key or subscription is needed. The default server is
https://photon.komoot.io/; change Photon:BaseUrl (or Photon__BaseUrl) to another trusted
HTTPS Photon instance if necessary. Provider response shapes are not exposed directly.

## Requests
Text search: /api/places/search?query=Colon&latitude=39.47&longitude=-0.376&limit=5
Restricted area: add &boundingBox=-0.44,39.42,-0.30,39.52
Nearby pharmacies: /api/places/search?category=pharmacy&latitude=39.47&longitude=-0.376&radiusKilometers=3&limit=5
Latitude/longitude bias text results, but do not restrict them; use boundingBox to restrict.
Radius applies only to category search. Only pharmacy is supported as a category for now.
All query parameter names are English. A translated category button sends pharmacy;
a free-text query does not automatically classify the word farmacia.

## Results and saving
200 returns an array of providerPlaceId, label, address, latitude and longitude.
An empty array is a successful search without matches. Address may be null.
Coordinates are mapped from GeoJSON longitude/latitude and rounded to six decimals.
Labels/addresses are capped at the existing Location limits (150/500 characters).
Provider IDs use osm:type:id and are external references, not persisted Location IDs.
Show candidates, let the user confirm on the map, and copy label/address/latitude/longitude
into the existing POST /api/plans/{planId}/errands newLocation object. No search writes to
MariaDB. Search names use Photon default/local names, not an assumed Spanish translation.

## Errors and operational limits
400 validation_failed: invalid or conflicting input; field errors use camelCase.
401/403: existing authentication/registration policy.
429 rate_limit_exceeded: local 30 requests/minute per authenticated user, no request queue.
503 place_search_unavailable: provider/network/JSON/timeout failure or overlapping search.
A 503 is not an empty search. Keep existing form data and let the user retry explicitly.
Do not automatically retry aggressively. Cancel obsolete searches, debounce typing and
handle errors in Angular. The provider gate limits all users together within one process
(one active request, at least one second between starts). This is our conservative policy,
not a guaranteed Photon allowance. Multiple API instances would need coordinated limits.
Outbound HttpClient URI logging is disabled; do not enable request-query logging elsewhere.
Provider requests have a ten-second timeout and one-MiB response limit.

## Attribution and service conditions
Show © OpenStreetMap contributors with https://www.openstreetmap.org/copyright in the
search/map UI. Photon is based on OSM; coverage and address completeness vary by location.
Public service use is allowed only within reasonable limits, without availability guarantees.
Sources: https://github.com/komoot/photon and
https://github.com/komoot/photon/blob/master/docs/api-v1.md
Legacy osm_tag is documented but deprecated upstream; verify filters when changing servers.

## Manual verification
Use backend/src/ErrandFlow.Api/Places.http with local baseUrl/accessToken variables.
Verify text and pharmacy queries, empty results, invalid limits/coordinates, both query and
category, missing coordinates for pharmacy, unknown category, and invalid bounding boxes.
Verify unauthorized requests and 429. On a disposable local configuration use an unavailable
trusted HTTPS endpoint to verify 503. Verify search never changes any plan revision.
No migration or SQL execution is required to add this feature.
