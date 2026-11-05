-- Bootstrap the currently implemented Users, Plans, Locations and Errands model
-- in an EMPTY database.
-- Select the target database explicitly with the MariaDB client. Do not run on an existing schema.
-- DDL auto-commits: this script is not an atomic migration.
SET SESSION time_zone = '+00:00';

CREATE TABLE users (
    id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    identity_user_id VARCHAR(255) CHARACTER SET ascii COLLATE ascii_bin NULL,
    display_name VARCHAR(100) NOT NULL,
    preferred_language VARCHAR(5) NOT NULL DEFAULT 'es',
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (id),
    UNIQUE KEY uq_users_identity (identity_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE plans (
    id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    owner_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    name VARCHAR(150) NOT NULL,
    plan_date DATE NOT NULL,
    departure_time TIME NOT NULL,
    time_zone VARCHAR(100) NOT NULL DEFAULT 'Europe/Madrid',
    origin_location_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
    destination_location_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
    revision BIGINT UNSIGNED NOT NULL DEFAULT 1,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    PRIMARY KEY (id),
    KEY ix_plans_owner_date (owner_id, plan_date),
    CONSTRAINT fk_plans_owner FOREIGN KEY (owner_id) REFERENCES users (id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ck_plans_name CHECK (CHAR_LENGTH(TRIM(name)) > 0),
    CONSTRAINT ck_plans_zone CHECK (CHAR_LENGTH(TRIM(time_zone)) > 0),
    CONSTRAINT ck_plans_departure CHECK (departure_time >= '00:00:00' AND departure_time < '24:00:00'),
    CONSTRAINT ck_plans_revision CHECK (revision > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE locations (
    id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    plan_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    label VARCHAR(150) NOT NULL,
    address VARCHAR(500) NULL,
    latitude DECIMAL(9, 6) NOT NULL,
    longitude DECIMAL(9, 6) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_locations_plan_id (plan_id, id),
    CONSTRAINT fk_locations_plan FOREIGN KEY (plan_id) REFERENCES plans (id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ck_locations_label CHECK (CHAR_LENGTH(TRIM(label)) > 0),
    CONSTRAINT ck_locations_latitude CHECK (latitude BETWEEN -90 AND 90),
    CONSTRAINT ck_locations_longitude CHECK (longitude BETWEEN -180 AND 180)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE errands (
    id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    plan_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    location_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    title VARCHAR(150) NOT NULL,
    duration_minutes SMALLINT UNSIGNED NOT NULL,
    original_position INT UNSIGNED NOT NULL,
    earliest_start_at DATETIME(6) NULL,
    latest_finish_at DATETIME(6) NULL,
    fixed_start_at DATETIME(6) NULL,
    created_at DATETIME(6) NOT NULL,
    updated_at DATETIME(6) NOT NULL,
    PRIMARY KEY (id),
    UNIQUE KEY uq_errands_position (plan_id, original_position),
    KEY ix_errands_location (plan_id, location_id),
    CONSTRAINT fk_errands_plan FOREIGN KEY (plan_id) REFERENCES plans (id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT fk_errands_location FOREIGN KEY (plan_id, location_id)
        REFERENCES locations (plan_id, id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ck_errands_title CHECK (CHAR_LENGTH(TRIM(title)) > 0),
    CONSTRAINT ck_errands_duration CHECK (duration_minutes > 0),
    CONSTRAINT ck_errands_window CHECK (earliest_start_at IS NULL OR latest_finish_at IS NULL OR earliest_start_at < latest_finish_at),
    CONSTRAINT ck_errands_fixed_start CHECK (fixed_start_at IS NULL OR earliest_start_at IS NULL OR fixed_start_at >= earliest_start_at),
    CONSTRAINT ck_errands_fixed_finish CHECK (fixed_start_at IS NULL OR latest_finish_at IS NULL OR fixed_start_at + INTERVAL duration_minutes MINUTE <= latest_finish_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add the reverse references after both tables exist.
ALTER TABLE plans
    ADD KEY fk_plans_origin (id, origin_location_id),
    ADD KEY fk_plans_destination (id, destination_location_id),
    ADD CONSTRAINT fk_plans_origin
        FOREIGN KEY (id, origin_location_id) REFERENCES locations (plan_id, id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    ADD CONSTRAINT fk_plans_destination
        FOREIGN KEY (id, destination_location_id) REFERENCES locations (plan_id, id)
        ON DELETE RESTRICT ON UPDATE RESTRICT;
