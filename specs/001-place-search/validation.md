# Evidence
Date: 2026-11-05. SDK used for checks: 10.0.403 (temporary environment installation).

| Requirement | Check | Result | Evidence |
| --- | --- | --- | --- |
| FR-1/2/3/7 | Compile backend | PASS | dotnet build backend/src/ErrandFlow.Api/ErrandFlow.Api.csproj --no-restore -m:1 --nologo: 0 warnings, 0 errors |
| FR-3 | Invalid modes, query length, coordinates, category, bounds, limit | PASS | Temporary isolated .NET harness; invalid input never calls the fake provider |
| FR-1/2 | GeoJSON axis order, external ID and invariant URI encoding | PASS | Temporary harness links actual Application/Infrastructure source, with fake HTTP responses |
| FR-5 | HTTP 429 upstream, invalid JSON, cancellation, overlapping requests | PASS | Isolated harness; empty successful results distinguished from errors |
| FR-1 | Typed HttpClient DI | PASS | Resolved real adapter through service collection with URI logging disabled |
| FR-4 | Persistence isolation | PASS (source review) | No DbContext or repository dependency in Places feature |
| FR-6 | Actual authenticated 429 response | NOT RUN | Requires running API with local JWT/database configuration |
| FR-1/2/3/5/7 | Authenticated HTTP endpoint, model binding, real timeout and 503 responses | NOT RUN | Requires local configuration; manual requests supplied |
| All | git diff --check | PASS | No whitespace errors |

The isolated harness executed 20 assertions, not the repository's automated test suite.
It was temporary verification only; no permanent test project was introduced because
project tests remain deferred. Real endpoint smoke evidence, if available, is noted below.

Live provider smoke: PASS. The actual C# adapter returned five normalized text results
within a Valencia bounding box and five nearby pharmacy results from the public Photon
server. This verifies those requests only, not service availability or all geographic coverage.

## Unified category search extension (2026-11-05)
- PASS FR-8/9/10: 61 isolated source-linked checks using temporary .NET SDK 10.0.403.
  Coverage: accented/case/plural/whitespace ES/EN aliases, exact matching, named places,
  unknown text fallback, legacy category, text bounding box, conflicting input, missing
  center, invalid geography/radius, all 18 category mappings and escaped text requests.
  Command: dotnet run --project /tmp/place-checks/PlaceChecks.csproj --no-restore.
  This temporary harness is not a permanent repository test suite.
- PASS: dotnet build backend/src/ErrandFlow.Api/ErrandFlow.Api.csproj -m:1 --nologo;
  zero warnings and errors. PASS: git diff --check.
- PASS live adapter smoke: query=panadería, query=supermarket and query=Mercadona each
  returned five normalized candidates. An earlier attempt timed out; the repeat succeeded.
  This verifies those requests at test time, not provider uptime or all category coverage.
- NOT RUN: authenticated HTTP/model-binding/rate-limit checks with the user's deployment.
  Updated Places.http contains manual examples. No database changes or migrations.
