# ErrandFlow · Mobile plan editor UX

**Reviewed source:** `Vanest7/errandflow`, branch `develop`, commit `79b411c16e3b38aa7979abae3c72c48e0ba02810` (2026-11-10).  
**Scope:** frontend UI only. No backend, database, API contracts or new dependencies.

## Install

Extract this ZIP in your ErrandFlow repository root. The expected path is `errandflow-mobile-ui/apply_mobile_ui.py`.

Before applying, commit or otherwise back up your working changes. Then run:

```bash
python3 errandflow-mobile-ui/apply_mobile_ui.py --root . --check
python3 errandflow-mobile-ui/apply_mobile_ui.py --root .
```

The script validates all source anchors before writing. `--check` is read-only; a second installation reports `ALREADY APPLIED` and makes no changes. A partially applied installation is rejected rather than silently duplicated. If your files have diverged from the reviewed commit, it stops with an error and does not write partial changes.

## What changes

- Small viewports (`<1024px`): a sticky, accessible **Errands / Map and route** selector prevents one overlong editor screen. The panels stay mounted, preserving form state and OpenLayers state.
- Desktop (`>=1024px`): the existing side-by-side layout remains visible.
- Selecting a point on the map reveals the original errand or endpoint form again; choosing a map location opens the map automatically.
- Successful route calculation opens the map view on mobile.
- OpenLayers' map resizes when revealed and re-fits existing route/markers only after a hidden-to-visible transition.
- Editing/deleting/reordering controls gain a minimum 44px touch target, without adding hover-only labels to edit/delete.
- Dragging has a 200ms touch start delay; the existing position select remains available for touch and keyboard.
- Map height and comparison-card spacing adapt to narrow screens.
- Adds `plans.mobile` keys for Spanish and English; existing translation keys are preserved.

## Changed app files

```text
frontend/src/app/features/plans/pages/plan-editor/plan-editor.ts
frontend/src/app/features/plans/pages/plan-editor/plan-editor.html
frontend/src/app/shared/map/components/route-map/route-map.ts
frontend/src/app/shared/map/components/route-map/route-map.html
frontend/src/app/features/plans/components/route-comparision/route-comparision.html
frontend/public/i18n/es.json
frontend/public/i18n/en.json
```

The `route-comparision` spelling matches the existing repository folder. The ZIP does not rename it.

## Verify locally

```bash
cd frontend
npm run build:dev
npm run lint
npm run test:ci
```

Test at ~320px, 375px, 768px, and >=1024px, in both light/dark and ES/EN:

1. Switch Errands / Map and route; editing forms should retain entered content.
2. From an errand or endpoint picker, choose **On the map**, place a point, and confirm that the editing form becomes visible again.
3. Calculate a walking route, then a driving route. On mobile, results should open in the map view. The map geometry should render correctly after switching away and back.
4. Edit, delete, set a schedule and change position using the select control. Buttons should be at least 44px in each dimension; vertical scrolling outside the drag handle should work naturally.
5. Verify the route comparison cards fit without horizontal scrolling.
6. At >=1024px, verify both panels are visible simultaneously.

## Verification evidence

- Verified: Python syntax and installer consistency against a captured user HTML template and synthetic matching TypeScript/templates; read-only preparation, application, reapplication, and partial-install refusal.
- **Not verified here:** Angular build/lint/test suite with local dependencies, browser behavior, live map/route service or mobile devices. Run the commands and manual checks above on your checkout.
