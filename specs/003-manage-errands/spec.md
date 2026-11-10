# Errand management: edit, delete, reorder

## Scope
- EM-01: When a user edits an existing errand, the form prefills its title and duration and retains its location unless a different place/map point is selected.
- EM-02: When a user confirms deletion, the app deletes only the selected errand and updates the plan display.
- EM-03: When a user moves a stop up or down, the app saves the complete original-order list, preserving all errands exactly once.
- EM-04: After any edit, deletion, or reorder attempt, previously calculated map paths and schedules must not be represented as current.
- EM-05: The app handles stale plan revisions with a localized reload action and prevents overlapping mutations.
- EM-06: Controls must have accessible names, keyboard operation, and matching Spanish/English translations.

## Boundaries
The existing backend `PUT /errands/{id}`, `DELETE /errands/{id}` and `PUT /errands/order` contracts are reused through the generated Angular client. No API changes, migrations or new packages.

## Deferred
Editing fixed appointments and opening windows is a separate scheduling feature using `PUT /errands/{id}/schedule` and explicit time-zone/DST validation. Drag-and-drop can follow the accessible up/down controls.
