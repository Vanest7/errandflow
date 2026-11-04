---
name: errandflow-frontend
description: Build or review ErrandFlow Angular screens, typed API integration, responsive
  maps and Transloco ES/EN interfaces.
---

# Angular feature workflow
Read frontend/AGENTS.md and the active feature requirements.
1. Inspect existing conventions and dependency versions; reuse established components/state.
2. Place orchestration in a page/facade and display behavior in focused components.
3. Put typed HTTP integration in data-access. Handle loading, empty, error and retry states.
4. Add matching ES/EN keys; preserve technical identifiers and localize backend codes in the UI.
5. Provide keyboard access, labels and a usable textual itinerary alongside the map.
6. For origin selection, request location on user action; handle denial, timeout and manual input.
7. Check narrow mobile and desktop layouts, translated text expansion and keyboard focus.
8. Run configured type/build/lint checks and behavior tests relevant to the change.
Keep graph optimization on the backend. Do not add libraries just to avoid a small component.
Output: user-visible behavior, translation keys, states and verification evidence.
