# ErrandFlow Frontend Architecture

**Status:** Agreed target architecture. Adopt incrementally; the sections below do not claim that every feature already exists.

**Scope:** `frontend/` (Angular application). Read alongside `../docs/architecture.md`, `../docs/constitution.md`, root `AGENTS.md` and `frontend/AGENTS.md`.

## 1. Goals and principles

- Build a responsive, accessible and maintainable Angular frontend for plans, errands, appointments, locations and optimized routes.
- Organize source code **by feature** rather than by a global collection of components and services.
- Separate presentation, feature state, data access and generated API contracts.
- Use small, explicit abstractions. Avoid speculative complexity and unnecessary architectural layers.
- Keep one Angular application in the current Nx workspace. Extract an Nx library only when actual reuse or a meaningful dependency boundary justifies it.
- Prevent cyclic dependencies and importing another feature's private implementation.
- Use English for source code, comments, identifiers, tests and technical documentation. Localize user-visible text into Spanish and English.
- Create folders and abstractions when a feature actually needs them; do not scaffold empty layers.

## 2. Target structure

```text
frontend/
├── ARCHITECTURE.md
├── AGENTS.md
├── openapi/
│   └── errandflow.json               # OpenAPI snapshot if file-based generation is used
└── src/
    └── app/
        ├── core/
        │   ├── api/
        │   │   └── generated/         # OpenAPI-generated Angular client; do not edit
        │   ├── auth/                  # Auth0 and local-user registration
        │   ├── config/                # Backend base URL and runtime configuration
        │   └── http/                  # Shared HTTP behavior and error interpretation
        ├── shared/
        │   ├── ui/                    # Reusable presentational components
        │   ├── map/                   # Map rendering, adapters, location picker
        │   └── utils/                 # Pure feature-independent helpers
        ├── features/
        │   ├── plans/
        │   │   ├── pages/
        │   │   ├── components/
        │   │   ├── data-access/
        │   │   ├── state/             # Only when reactive feature state is needed
        │   │   └── models/            # Only when view models differ from API DTOs
        │   ├── planning/
        │   │   ├── pages/
        │   │   ├── components/
        │   │   ├── data-access/
        │   │   ├── state/
        │   │   └── models/
        │   └── settings/              # Add subfolders as needed
        ├── app.config.ts
        ├── app.routes.ts
        └── app.ts
```

This is a **target structure**, not a request to create all directories now. Co-locate unit tests with the code they verify.

## 3. Responsibilities and dependency boundaries

| Area                 | Owns                                                                          | Must not own                                    |
| -------------------- | ----------------------------------------------------------------------------- | ----------------------------------------------- |
| `core/api/generated` | Generated OpenAPI HTTP client and DTOs                                        | Business decisions or manual patches            |
| `core/auth`          | Auth0 login/logout, access-token integration, registered-user synchronization | Feature-specific UI or routing algorithms       |
| `core/config`        | Typed configuration and API base URL                                          | URLs hard-coded throughout features             |
| `core/http`          | Cross-cutting HTTP behavior and stable API-error interpretation               | Feature-specific UX decisions                   |
| `shared/ui`          | Reusable presentational components                                            | Data fetching or mutable feature stores         |
| `shared/map`         | Map adapters, rendering and point selection                                   | Route optimization or schedule feasibility      |
| `features/plans`     | Plans, errands, locations and origin/destination editing                      | Backend graph optimization algorithms           |
| `features/planning`  | Requesting and displaying graph, route, scheduling and optimization results   | Reimplementing authoritative backend algorithms |
| `features/settings`  | Language and supported user preferences                                       | Auth implementation details                     |

### Dependency rules

- Feature pages and components may depend on their own state, data-access facades, models, `shared` and public `core` interfaces.
- **HTTP communication belongs in data-access** (or central auth integration inside `core/auth`); feature UI components must not inject `HttpClient` or generated API services directly.
- Only data-access/adapters and narrowly scoped `core` integration code import generated API clients.
- Feature state may depend on its feature's data-access contracts, not on direct HTTP URLs.
- `shared` must not import from `features` or depend on generated backend DTOs.
- Do not import other features' private pages, state or data access. Move genuinely reusable independent capabilities into `shared` or a small public contract when needed.
- The backend is authoritative for authorization, revisions, graph weights, schedule feasibility and route optimization.

Expected request flow:

```text
Feature Page / UI Component
            ↓
Feature State (Signals, when needed)
            ↓
Feature Data-Access Service
            ↓
Generated OpenAPI Client
            ↓
ASP.NET Core API
```

For a simple read-only page, calling a feature data-access service directly is acceptable. A store is **not mandatory**.

## 4. OpenAPI and data access

- Use ASP.NET Core's published OpenAPI contract (`/openapi/v1.json` in the current Development setup), not a handwritten schema.
- Generate typed Angular services and DTOs using `ng-openapi-gen` after verifying the API contract.
- Place generated output under `src/app/core/api/generated/` and treat it as disposable/reproducible. **Never edit generated files manually.**
- Keep the generator configuration at the frontend root and document the regeneration command. Regenerate when backend HTTP contracts change.
- The generator reads the committed snapshot `openapi/errandflow.json` through `ng-openapi-gen.json`. With the Development backend running, `npm run api:update` refreshes the snapshot and regenerates the client; `npm run api:generate` regenerates from the snapshot alone.
- The generated folder and OpenAPI snapshot are excluded from linting and formatting because they are reproducible artifacts.
- Centralize the backend base URL in `src/app/core/config/api.config.ts`. The generated client receives it through `provideApiConfiguration()`; do not hard-code that address inside pages or feature services.
- Generated DTOs are transport contracts. Introduce dedicated UI models and mapping functions **only** when the presentation representation meaningfully differs.
- Model nullable/optional fields explicitly. Avoid `any`, broad casts and unjustified non-null assertions.
- Never commit access tokens or private secrets. Public OAuth client identifiers are not secrets, but should be centrally configured.

## 5. Authentication and HTTP errors

- Keep Auth0 configuration, login/logout and authorized request handling centralized in `core/auth` and app configuration.
- Ensure the API access token is attached only to requests targeting the configured backend origin.
- Move the `/api/auth/me` registration/synchronization flow out of `app.ts` into a dedicated auth facade/service.
- Keep `app.ts` focused on application shell responsibilities and router display, not user registration or raw HTTP calls.
- Distinguish 401 (authentication), 403 (authorization), 404 (not found), 409 (revision conflict), 429 (rate limiting), and 503 (provider unavailable).
- Treat backend error codes as stable machine values; translate user-facing explanations, not codes.
- Send `ExpectedRevision` on applicable mutations and update local state using the returned plan revision. On 409, refresh/reconcile rather than silently overwriting edits.
- Never log credentials, access tokens, precise GPS coordinates or sensitive appointment contents.

## 6. State and asynchronous flows

- Prefer Angular **Signals** for view/feature state and `computed()` for derived values.
- Use **RxJS** for HTTP, cancellation, debounced searches and genuinely asynchronous streams.
- Use Angular's observable cleanup and interoperability facilities; avoid unmanaged subscriptions.
- Choose one state owner per feature/screen. Do not introduce NgRx, SignalStore or competing stores without a demonstrated need.
- When a facade is useful, expose read-only signals such as `items`, `isLoading`, `error` and `selectedId`, with explicit methods like `load()` and `save()`.
- Represent loading, empty, success and error states explicitly. Do not swallow failures silently.
- Avoid copying the same mutable source of truth into several components.

## 7. Routing, UI and accessibility

- Use standalone Angular components and lazy-loaded routes for independent feature screens.
- Keep components focused. Pages/state orchestrate; presentational components render and emit user intents.
- Use Angular Material for accessible interaction patterns and Tailwind for layout/utilities, with consistent design tokens.
- Keep screen-specific components inside their feature. Move to `shared/ui` only after real cross-feature reuse.
- Support narrow and wide viewports, keyboard access, form labels, visible focus and localized feedback.
- Always offer list/form alternatives to map-only workflows, including location selection and itinerary inspection.

## 8. Maps, GPS and planning

- Keep map-library/provider integration in `shared/map`. Feature code supplies display data and handles selected coordinates or places.
- Put place/category search and persistence behind feature data-access services, not inside map-rendering components.
- GPS is **optional, one-shot and used only to suggest the initial origin**; allow manual entry and handle denied permission.
- Do not implement continuous tracking as part of the MVP.
- Display backend-provided route geometry and travel times; do not represent straight-line connections as road routes.
- Distinguish original user order from optimized order and present waiting periods and schedule conflicts accurately.
- Call a route optimal only when the backend reports that optimality was proven for the stated objective.
- Expose only travel modes actually supported by the backend.

## 9. Internationalization and time semantics

- Use **Transloco** with Spanish (`es`) and English (`en`) JSON dictionaries after integration.
- Translate all visible copy, validation feedback, aria labels and actionable error messages; keep both dictionaries in sync.
- Never translate IDs, stable backend error codes or protocol values.
- Use locale-aware formatting for dates, numbers and times. Display or clearly communicate the plan's declared time zone.
- Distinguish UTC instants from local plan dates and departure times; follow backend time semantics.
- Do not invent commercial opening hours or appointment windows not supplied by the backend/user.

## 10. Verification and evolution

- Keep strict TypeScript and typed API boundaries.
- Add focused tests for state transitions, mappers, forms and error handling when building those features.
- Verify auth redirect/registration, revision conflicts, API errors, ES/EN text expansion, mobile/desktop layouts, keyboard flow and denied GPS.
- Use scripts actually present in `frontend/package.json`; currently `npm run build`, `npm run lint` and `npm run test:ci` are defined. Report actual results, never assume checks passed.
- Do not add Nx libraries, microfrontends, advanced state frameworks or extra map abstractions for hypothetical future scaling.
- Record material architecture changes with context, decision and consequences under `docs/decisions/`. Keep this document synchronized with implementation.

## 11. Current code and next implementation steps

As of 2026-11-06:

- `core/auth` owns Auth0 configuration, login/logout, local-user registration and API identity synchronization. `app.ts` only composes the shell and router outlet.
- The typed OpenAPI client is generated into `core/api/generated/` from `openapi/errandflow.json` using `ng-openapi-gen`.
- The API base URL is defined once in `core/config/api.config.ts` and supplied to the generated client.

Next:

1. Add the first `plans` feature page and data-access service. Introduce Signals state only as needed.
2. Add lazy feature routes, Transloco ES/EN and the map adapter when the related functionality is built.

This document defines boundaries and intended responsibilities; it is **not evidence** that the target folders and features are implemented.
