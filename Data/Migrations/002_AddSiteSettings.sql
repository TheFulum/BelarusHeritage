-- ============================================================
--  002 — Добавление таблицы site_settings
--  Накатывается на существующую БД (DB schema до этой миграции
--  не содержит site_settings, хотя модель и сидер её уже ожидают).
-- ============================================================

USE belarus_heritage;
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS site_settings (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    `key`       VARCHAR(100) NOT NULL,
    `value`     TEXT         NULL,
    updated_at  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE INDEX idx_site_settings_key (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
