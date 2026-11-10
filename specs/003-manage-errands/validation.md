# Verification

- Source contracts: PASS (verified against develop `1b8b1ed`): generated methods and DTOs for update, delete, reorder.
- Installer Python syntax: PASS (`python3 -m py_compile`).
- Installer dry-run against the user's working tree: NOT RUN (local files unavailable).
- Angular build, lint and Vitest: NOT RUN (user's Node dependencies and workspace unavailable).
- Authenticated browser edit/delete/reorder with real MariaDB: NOT RUN.
- Schedule editing: OUT OF SCOPE; requires separate use case and time-zone handling.
- Installer fixture check: PASS (synthetic input with expected anchors); dry-run, installation, rerun, and preservation of a pre-existing itinerary block and translations.
- Reorder helper isolated TypeScript check: PASS using global TypeScript 5.8 with matching generated DTO stub. This is not an Angular workspace build.
