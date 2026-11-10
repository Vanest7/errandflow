# Validation status

- SCHEDULE-01 / SCHEDULE-02: Source implemented; Angular compilation and browser interaction are NOT RUN in the user's environment.
- SCHEDULE-03 / SCHEDULE-04: Isolated time conversion checks can be run without Angular. Vitest specs are bundled, but they must be executed in the user's repo.
- SCHEDULE-05 / SCHEDULE-06: API contract verified from the current GitHub repository; live backend and concurrent update tests NOT RUN.
- Installer: Tested in a synthetic tree matching the previously distributed management and UI scripts; this does not prove compatibility with uncommitted local differences.
