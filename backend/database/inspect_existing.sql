-- Read-only inspection. Select the existing database explicitly before executing.
SELECT DATABASE() AS database_name, VERSION() AS server_version,
       @@session.time_zone AS session_time_zone, @@global.time_zone AS global_time_zone;
SHOW CREATE TABLE users;
SHOW CREATE TABLE plans;
SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT, COLLATION_NAME
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME IN ('users', 'plans', 'locations', 'errands')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
SELECT TABLE_NAME, CONSTRAINT_NAME, REFERENCED_TABLE_NAME, UPDATE_RULE, DELETE_RULE
FROM information_schema.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE();
SELECT COUNT(*) AS orphan_plan_count
FROM plans p LEFT JOIN users u ON u.id = p.owner_id WHERE u.id IS NULL;
