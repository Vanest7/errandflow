# Frontend rules

Apply root AGENTS.md first.

- Organize app/core, app/shared and app/features/{plans,planning,settings}.
- Within features, create pages, components, data-access and models only when needed.
- Keep map adapters in shared/map; map components display state, not optimization rules.
- Keep HTTP calls in data-access. Reuse the chosen state pattern; avoid competing state stores.
- Use strict TypeScript, typed API DTOs and explicit loading, empty, success and error states.
- Translate user-facing text through Transloco ES/EN dictionaries; keep matching key sets.
- Format dates/numbers using locale. Never translate IDs or stable backend error codes.
- Support keyboard navigation, labels, visible focus and a list alternative to map-only content.
- Verify narrow mobile and desktop layouts, text expansion and geolocation denial.
- GPS is one-shot and optional; provide manual origin entry. Do not add continuous tracking.
- Inspect package.json and lockfile for package manager, Angular version and actual scripts.
