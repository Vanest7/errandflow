-- Bootstrap the currently implemented Users and Plans model in an EMPTY database.
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
        ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
