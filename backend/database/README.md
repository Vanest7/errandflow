# Database baseline

The versioned SQL baseline targets MariaDB 11.8.6 and the currently implemented Users and
Plans EF model. It does not reconstruct an uninspected development database or implement
future Locations/Errands features. Origin/destination IDs remain nullable placeholders.

## New installation

An administrator must provision an empty database and a least-privilege application account.
Select that database explicitly and run `001_initial.sql` once with a schema-management account.
The script intentionally has no `IF NOT EXISTS`, destructive statements or automatic startup hook.
DDL auto-commits; inspect partial state if execution fails rather than rerunning blindly.
Configure MariaDB connection sessions to use UTC for database-generated user timestamps.
The bootstrap session's UTC setting does not configure future application connections.

## Existing installation

Do NOT execute the bootstrap against an existing database. First back up the database and run
`inspect_existing.sql` against it. Compare types, collations, defaults, indexes and foreign keys
with the baseline and EF mappings. Include any existing Locations/Errands constraints in that
review. An existing equivalent owner foreign key may have a different name and must not be
added twice. Resolve orphan plans deliberately before adding a missing owner foreign key.

No existing-schema ALTER script is supplied without inspecting that schema. Record the reviewed
incremental change as the next numbered script, validate it on a restored copy, then deploy it.
Do not use EnsureCreated or automatic migrations on startup. If EF migrations are adopted later,
establish a reviewed baseline first; do not apply a generated initial migration to existing tables.

## Verification

SQL execution and comparison with the user's database have NOT RUN in this environment.
The checked-in SQL provides a reproducible baseline, not evidence of a completed live migration.
