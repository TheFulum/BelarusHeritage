-- ============================================================
--  КУЛЬТУРНОЕ НАСЛЕДИЕ БЕЛАРУСИ — MySQL 8.x Initial Migration
--  Generated from Entity Framework Core Models
--  For manual database creation
-- ============================================================

CREATE DATABASE IF NOT EXISTS belarus_heritage
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE belarus_heritage;

-- Force connection charset for this session so INSERT statements with
-- Cyrillic strings don't get mangled by Windows code page on mysql.exe.
SET NAMES utf8mb4 COLLATE utf8mb4_unicode_ci;

-- ============================================================
--  REFERENCE TABLES
-- ============================================================

-- Regions (6 oblasts + Minsk city)
CREATE TABLE regions (
    id          TINYINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    code        VARCHAR(10)  NOT NULL UNIQUE,
    name_ru     VARCHAR(100) NOT NULL,
    name_be     VARCHAR(100) NOT NULL,
    name_en     VARCHAR(100) NOT NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Categories (замок, церковь, усадьба...)
CREATE TABLE categories (
    id          SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    slug        VARCHAR(60)  NOT NULL UNIQUE,
    name_ru     VARCHAR(100) NOT NULL,
    name_be     VARCHAR(100) NOT NULL,
    name_en     VARCHAR(100) NOT NULL,
    icon_class  VARCHAR(60)  NULL,
    color_hex   VARCHAR(7)   NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tags
CREATE TABLE tags (
    id          SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    slug        VARCHAR(80)  NOT NULL UNIQUE,
    name_ru     VARCHAR(100) NOT NULL,
    name_be     VARCHAR(100) NOT NULL,
    name_en     VARCHAR(100) NOT NULL,
    color_hex   VARCHAR(7)   NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Architectural styles
CREATE TABLE arch_styles (
    id          SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    slug        VARCHAR(80)  NOT NULL UNIQUE,
    name_ru     VARCHAR(100) NOT NULL,
    name_be     VARCHAR(100) NOT NULL,
    name_en     VARCHAR(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  HERITAGE OBJECTS
-- ============================================================

CREATE TABLE heritage_objects (
    id                  INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    slug                VARCHAR(160)  NOT NULL UNIQUE,
    name_ru             VARCHAR(255)  NOT NULL,
    name_be             VARCHAR(255)  NOT NULL,
    name_en             VARCHAR(255)  NOT NULL,
    category_id         SMALLINT UNSIGNED NOT NULL,
    region_id           TINYINT UNSIGNED  NOT NULL,
    arch_style_id       SMALLINT UNSIGNED NULL,
    century_start       SMALLINT      NULL,
    century_end         SMALLINT      NULL,
    build_year          SMALLINT      NULL,
    description_ru      LONGTEXT      NULL,
    description_be      LONGTEXT      NULL,
    description_en      LONGTEXT      NULL,
    short_desc_ru       VARCHAR(300)  NULL,
    short_desc_be       VARCHAR(300)  NULL,
    short_desc_en       VARCHAR(300)  NULL,
    fun_fact_ru         VARCHAR(500)  NULL,
    fun_fact_be         VARCHAR(500)  NULL,
    fun_fact_en         VARCHAR(500)  NULL,
    architect           VARCHAR(255)  NULL,
    heritage_category   TINYINT UNSIGNED NULL,
    heritage_year       YEAR    NULL,
    preservation_status ENUM('preserved','partial','ruins','lost') NOT NULL DEFAULT 'preserved',
    is_visitable        BOOLEAN NOT NULL DEFAULT TRUE,
    visiting_hours      VARCHAR(255)  NULL,
    entry_fee           VARCHAR(100)  NULL,
    main_image_url      VARCHAR(500)  NULL,
    status              ENUM('draft','published','archived') NOT NULL DEFAULT 'draft',
    is_deleted          BOOLEAN NOT NULL DEFAULT FALSE,
    is_featured         BOOLEAN NOT NULL DEFAULT FALSE,
    rating_avg          DECIMAL(3,2) NULL,
    rating_count        INT UNSIGNED NOT NULL DEFAULT 0,
    created_by          INT           NULL,
    updated_by          INT           NULL,
    created_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at          TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    INDEX idx_obj_status (status, is_deleted),
    INDEX idx_obj_category (category_id),
    INDEX idx_obj_region (region_id),
    INDEX idx_obj_century (century_start, century_end),
    INDEX idx_obj_featured (is_featured, status),
    INDEX idx_obj_rating (rating_avg DESC),

    CONSTRAINT fk_obj_category FOREIGN KEY (category_id) REFERENCES categories(id),
    CONSTRAINT fk_obj_region FOREIGN KEY (region_id) REFERENCES regions(id),
    CONSTRAINT fk_obj_style FOREIGN KEY (arch_style_id) REFERENCES arch_styles(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  IMAGES
-- ============================================================

CREATE TABLE object_images (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    object_id   INT UNSIGNED  NOT NULL,
    url         VARCHAR(500)  NOT NULL,
    thumb_url   VARCHAR(500)  NULL,
    caption_ru  VARCHAR(300)  NULL,
    caption_be  VARCHAR(300)  NULL,
    caption_en  VARCHAR(300)  NULL,
    is_main     BOOLEAN NOT NULL DEFAULT FALSE,
    is_360      BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order  TINYINT UNSIGNED NOT NULL DEFAULT 0,
    uploaded_by INT           NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_img_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_img_object (object_id, sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  LOCATIONS
-- ============================================================

CREATE TABLE object_locations (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    object_id   INT UNSIGNED  NOT NULL UNIQUE,
    lat         DECIMAL(10,7) NOT NULL,
    lng         DECIMAL(10,7) NOT NULL,
    address_ru  VARCHAR(300)  NULL,
    address_be  VARCHAR(300)  NULL,
    address_en  VARCHAR(300)  NULL,
    map_zoom    TINYINT UNSIGNED NOT NULL DEFAULT 15,

    CONSTRAINT fk_loc_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_loc_coords (lat, lng)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  SOURCES / BIBLIOGRAPHY
-- ============================================================

CREATE TABLE object_sources (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    object_id   INT UNSIGNED  NOT NULL,
    type        ENUM('book','article','website','archive','museum','other') NOT NULL DEFAULT 'other',
    title       VARCHAR(500)  NOT NULL,
    author      VARCHAR(300)  NULL,
    publisher   VARCHAR(300)  NULL,
    year        YEAR          NULL,
    url         VARCHAR(1000) NULL,
    pages       VARCHAR(50)   NULL,
    sort_order  TINYINT UNSIGNED NOT NULL DEFAULT 0,

    CONSTRAINT fk_src_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_src_object (object_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  TAG MAPPING
-- ============================================================

CREATE TABLE object_tag_map (
    object_id   INT UNSIGNED      NOT NULL,
    tag_id      SMALLINT UNSIGNED NOT NULL,
    PRIMARY KEY (object_id, tag_id),

    CONSTRAINT fk_otm_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    CONSTRAINT fk_otm_tag FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE,
    INDEX idx_otm_tag (tag_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  OBJECT RELATIONS
-- ============================================================

CREATE TABLE object_relations (
    object_id   INT UNSIGNED NOT NULL,
    related_id  INT UNSIGNED NOT NULL,
    reason      VARCHAR(200) NULL,
    PRIMARY KEY (object_id, related_id),

    CONSTRAINT fk_rel_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    CONSTRAINT fk_rel_related FOREIGN KEY (related_id) REFERENCES heritage_objects(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  USERS (using ASP.NET Identity)
-- ============================================================

CREATE TABLE users (
    -- Identity PK
    id                      INT AUTO_INCREMENT PRIMARY KEY,

    -- ASP.NET Identity core columns
    username                VARCHAR(60)   NULL,
    normalized_username     VARCHAR(60)   NULL,
    email                   VARCHAR(255)  NULL,
    normalized_email        VARCHAR(255)  NULL,
    email_confirmed         BOOLEAN       NOT NULL DEFAULT FALSE,
    password_hash           VARCHAR(255)  NULL,
    security_stamp          VARCHAR(255)  NULL,
    concurrency_stamp       VARCHAR(255)  NULL,
    phone_number            VARCHAR(32)   NULL,
    phone_number_confirmed  BOOLEAN       NOT NULL DEFAULT FALSE,
    two_factor_enabled      BOOLEAN       NOT NULL DEFAULT FALSE,
    lockout_end             DATETIME(6)   NULL,
    lockout_enabled         BOOLEAN       NOT NULL DEFAULT FALSE,
    access_failed_count     INT           NOT NULL DEFAULT 0,

    -- Custom app fields
    role            VARCHAR(30)   NOT NULL DEFAULT 'user',
    display_name    VARCHAR(100)  NULL,
    avatar_url      VARCHAR(500)  NULL,
    bio             VARCHAR(500)  NULL,
    preferred_lang  CHAR(2)       NOT NULL DEFAULT 'ru',
    is_active       BOOLEAN       NOT NULL DEFAULT TRUE,
    google_id       VARCHAR(100)  NULL,
    last_login_at   TIMESTAMP     NULL,
    created_at      TIMESTAMP     DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMP     DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE KEY ux_users_normalized_username (normalized_username),
    UNIQUE KEY ux_users_normalized_email (normalized_email),
    INDEX idx_users_role (role),
    INDEX idx_users_email (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity Role table
CREATE TABLE user_roles (
    id                INT          AUTO_INCREMENT PRIMARY KEY,
    name              VARCHAR(256) NULL,
    normalized_name   VARCHAR(256) NULL,
    concurrency_stamp VARCHAR(255) NULL,

    UNIQUE KEY ux_user_roles_normalized_name (normalized_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity UserRole table
CREATE TABLE user_role_map (
    user_id     INT NOT NULL,
    role_id     INT NOT NULL,
    PRIMARY KEY (user_id, role_id),

    CONSTRAINT fk_urm_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_urm_role FOREIGN KEY (role_id) REFERENCES user_roles(id) ON DELETE CASCADE,
    INDEX idx_urm_role (role_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity UserClaim table
CREATE TABLE user_claims (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    user_id     INT          NOT NULL,
    claim_type  VARCHAR(255) NULL,
    claim_value LONGTEXT     NULL,

    CONSTRAINT fk_uc_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_uc_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity UserLogin table (external providers: Google etc.)
CREATE TABLE user_logins (
    login_provider        VARCHAR(128) NOT NULL,
    provider_key          VARCHAR(128) NOT NULL,
    provider_display_name VARCHAR(255) NULL,
    user_id               INT          NOT NULL,
    PRIMARY KEY (login_provider, provider_key),

    CONSTRAINT fk_ul_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_ul_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity RoleClaim table
CREATE TABLE role_claims (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    role_id     INT          NOT NULL,
    claim_type  VARCHAR(255) NULL,
    claim_value LONGTEXT     NULL,

    CONSTRAINT fk_rc_role FOREIGN KEY (role_id) REFERENCES user_roles(id) ON DELETE CASCADE,
    INDEX idx_rc_role (role_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ASP.NET Identity UserToken table (internal: 2FA, provider refresh etc.)
-- NB: this is separate from our custom app-level user_tokens below.
CREATE TABLE asp_user_tokens (
    user_id        INT          NOT NULL,
    login_provider VARCHAR(128) NOT NULL,
    name           VARCHAR(128) NOT NULL,
    value          LONGTEXT     NULL,
    PRIMARY KEY (user_id, login_provider, name),

    CONSTRAINT fk_aut_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User tokens
CREATE TABLE user_tokens (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT          NOT NULL,
    type        ENUM('email_verify','password_reset','refresh') NOT NULL,
    token       VARCHAR(255)  NOT NULL UNIQUE,
    expires_at  TIMESTAMP     NOT NULL,
    used_at     TIMESTAMP     NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_tok_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_tok_token (token),
    INDEX idx_tok_user (user_id, type)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  FAVORITES
-- ============================================================

CREATE TABLE favorites (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT          NOT NULL,
    object_id   INT UNSIGNED NOT NULL,
    added_at    TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uq_fav (user_id, object_id),

    CONSTRAINT fk_fav_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_fav_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_fav_user (user_id),
    INDEX idx_fav_object (object_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  COMMENTS
-- ============================================================

CREATE TABLE comments (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT           NOT NULL,
    object_id   INT UNSIGNED  NOT NULL,
    body        TEXT          NOT NULL,
    status      ENUM('pending','approved','rejected','spam') NOT NULL DEFAULT 'pending',
    is_deleted  BOOLEAN NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_com_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_com_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_com_object (object_id, status),
    INDEX idx_com_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  RATINGS
-- ============================================================

CREATE TABLE ratings (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT          NOT NULL,
    object_id   INT UNSIGNED NOT NULL,
    value       TINYINT UNSIGNED NOT NULL CHECK (value BETWEEN 1 AND 5),
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_rating (user_id, object_id),

    CONSTRAINT fk_rat_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_rat_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_rat_object (object_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Rating triggers
DELIMITER $$

CREATE TRIGGER trg_rating_after_insert
AFTER INSERT ON ratings FOR EACH ROW
BEGIN
    UPDATE heritage_objects
    SET rating_avg = (SELECT AVG(value) FROM ratings WHERE object_id = NEW.object_id),
        rating_count = (SELECT COUNT(*) FROM ratings WHERE object_id = NEW.object_id)
    WHERE id = NEW.object_id;
END$$

CREATE TRIGGER trg_rating_after_update
AFTER UPDATE ON ratings FOR EACH ROW
BEGIN
    UPDATE heritage_objects
    SET rating_avg = (SELECT AVG(value) FROM ratings WHERE object_id = NEW.object_id),
        rating_count = (SELECT COUNT(*) FROM ratings WHERE object_id = NEW.object_id)
    WHERE id = NEW.object_id;
END$$

CREATE TRIGGER trg_rating_after_delete
AFTER DELETE ON ratings FOR EACH ROW
BEGIN
    UPDATE heritage_objects
    SET rating_avg = (SELECT AVG(value) FROM ratings WHERE object_id = OLD.object_id),
        rating_count = (SELECT COUNT(*) FROM ratings WHERE object_id = OLD.object_id)
    WHERE id = OLD.object_id;
END$$

DELIMITER ;

-- ============================================================
--  ROUTES
-- ============================================================

CREATE TABLE routes (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT           NOT NULL,
    title       VARCHAR(255)  NOT NULL,
    description TEXT          NULL,
    is_public   BOOLEAN NOT NULL DEFAULT FALSE,
    share_token VARCHAR(32)   NULL UNIQUE,
    total_km    DECIMAL(8,1)  NULL,
    start_address VARCHAR(255) NULL,
    start_lat   DECIMAL(10,7) NULL,
    start_lng   DECIMAL(10,7) NULL,
    end_address VARCHAR(255) NULL,
    end_lat     DECIMAL(10,7) NULL,
    end_lng     DECIMAL(10,7) NULL,
    source_route_id INT NULL,
    source_route_title VARCHAR(255) NULL,
    source_route_share_token VARCHAR(32) NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_route_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_route_user (user_id),
    INDEX idx_route_public (is_public, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE route_stops (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    route_id    INT UNSIGNED  NOT NULL,
    object_id   INT UNSIGNED  NOT NULL,
    sort_order  TINYINT UNSIGNED NOT NULL DEFAULT 0,
    notes       VARCHAR(500)  NULL,
    UNIQUE KEY uq_stop (route_id, object_id),

    CONSTRAINT fk_stop_route FOREIGN KEY (route_id) REFERENCES routes(id) ON DELETE CASCADE,
    CONSTRAINT fk_stop_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE CASCADE,
    INDEX idx_stop_route (route_id, sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  QUIZZES
-- ============================================================

CREATE TABLE quizzes (
    id          SMALLINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    slug        VARCHAR(80)   NOT NULL UNIQUE,
    type        ENUM('image_guess','region_guess','dragdrop','century_guess','odd_one_out') NOT NULL,
    title_ru    VARCHAR(200)  NOT NULL,
    title_be    VARCHAR(200)  NOT NULL,
    title_en    VARCHAR(200)  NOT NULL,
    description_ru TEXT       NULL,
    description_en TEXT       NULL,
    cover_url   VARCHAR(500)  NULL,
    time_limit  SMALLINT UNSIGNED NULL,
    is_active   BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order  TINYINT UNSIGNED NOT NULL DEFAULT 0,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE quiz_questions (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    quiz_id     SMALLINT UNSIGNED NOT NULL,
    body_ru     VARCHAR(500) NULL,
    body_be     VARCHAR(500) NULL,
    body_en     VARCHAR(500) NULL,
    image_url   VARCHAR(500) NULL,
    sort_order  SMALLINT UNSIGNED NOT NULL DEFAULT 0,

    CONSTRAINT fk_qq_quiz FOREIGN KEY (quiz_id) REFERENCES quizzes(id) ON DELETE CASCADE,
    INDEX idx_qq_quiz (quiz_id, sort_order)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE quiz_answers (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    question_id INT UNSIGNED  NOT NULL,
    body_ru     VARCHAR(300)  NOT NULL,
    body_be     VARCHAR(300)  NOT NULL,
    body_en     VARCHAR(300)  NOT NULL,
    is_correct  BOOLEAN NOT NULL DEFAULT FALSE,
    sort_order  TINYINT UNSIGNED NOT NULL DEFAULT 0,

    CONSTRAINT fk_qa_question FOREIGN KEY (question_id) REFERENCES quiz_questions(id) ON DELETE CASCADE,
    INDEX idx_qa_question (question_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE quiz_results (
    id              INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id         INT               NULL,
    quiz_id         SMALLINT UNSIGNED NOT NULL,
    score           TINYINT UNSIGNED  NOT NULL,
    correct_count   TINYINT UNSIGNED  NOT NULL,
    total_count     TINYINT UNSIGNED  NOT NULL,
    time_spent_sec  SMALLINT UNSIGNED NULL,
    completed_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_qr_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    CONSTRAINT fk_qr_quiz FOREIGN KEY (quiz_id) REFERENCES quizzes(id) ON DELETE CASCADE,
    INDEX idx_qr_user (user_id),
    INDEX idx_qr_quiz (quiz_id, score DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  ORNAMENTS
-- ============================================================

CREATE TABLE ornaments (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT           NOT NULL,
    title       VARCHAR(200)  NULL,
    image_url   VARCHAR(500)  NOT NULL,
    is_public   BOOLEAN NOT NULL DEFAULT FALSE,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_orn_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_orn_user (user_id),
    INDEX idx_orn_public (is_public, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  TIMELINE
-- ============================================================

CREATE TABLE timeline_events (
    id          INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    object_id   INT UNSIGNED  NULL,
    year        SMALLINT      NOT NULL,
    title_ru    VARCHAR(300)  NOT NULL,
    title_be    VARCHAR(300)  NOT NULL,
    title_en    VARCHAR(300)  NOT NULL,
    body_ru     TEXT          NULL,
    body_en     TEXT          NULL,
    image_url   VARCHAR(500)  NULL,
    is_published BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order  SMALLINT UNSIGNED NOT NULL DEFAULT 0,

    CONSTRAINT fk_tl_object FOREIGN KEY (object_id) REFERENCES heritage_objects(id) ON DELETE SET NULL,
    INDEX idx_tl_year (year),
    INDEX idx_tl_object (object_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  AUDIT LOG
-- ============================================================

CREATE TABLE audit_log (
    id          BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    user_id     INT           NULL,
    action      VARCHAR(50)   NOT NULL,
    entity      VARCHAR(60)   NOT NULL,
    entity_id   INT UNSIGNED  NULL,
    payload     JSON          NULL,
    ip_address  VARCHAR(45)   NULL,
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT fk_audit_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE SET NULL,
    INDEX idx_audit_user (user_id),
    INDEX idx_audit_entity (entity, entity_id),
    INDEX idx_audit_date (created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Site settings (key-value хранилище для глобальных настроек: соцсети, имя сайта и т.п.)
CREATE TABLE site_settings (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    `key`       VARCHAR(100) NOT NULL,
    `value`     TEXT         NULL,
    updated_at  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    UNIQUE INDEX idx_site_settings_key (`key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  SEED DATA
-- ============================================================

-- Regions
INSERT INTO regions (code, name_ru, name_be, name_en) VALUES
('brest', 'Брестская область', 'Брэсцкая вобласць', 'Brest Region'),
('vitebsk', 'Витебская область', 'Віцебская вобласць', 'Vitebsk Region'),
('gomel', 'Гомельская область', 'Гомельская вобласць', 'Gomel Region'),
('grodno', 'Гродненская область', 'Гродзенская вобласць', 'Grodno Region'),
('minsk', 'Минская область', 'Мінская вобласць', 'Minsk Region'),
('mogilev', 'Могилёвская область', 'Магілёўская вобласць', 'Mogilev Region'),
('minsk_city', 'г. Минск', 'г. Мінск', 'Minsk City');

-- Categories
INSERT INTO categories (slug, name_ru, name_be, name_en, icon_class, color_hex) VALUES
('castle', 'Замок', 'Замак', 'Castle', 'bi bi-shield-check', '#8B1A2A'),
('church', 'Церковь', 'Царква', 'Church', 'bi bi-house', '#3B5E3F'),
('estate', 'Усадьба', 'Сядзіба', 'Estate', 'bi bi-house-door', '#7A5C1E'),
('katedral', 'Костёл', 'Касцёл', 'Cathedral', 'bi bi-building', '#2A4A7F'),
('monastery', 'Монастырь', 'Манастыр', 'Monastery', 'bi bi-columns', '#5B3A7A'),
('hillfort', 'Городище', 'Гарадзішча', 'Hillfort', 'bi bi-geo', '#4A6741'),
('mosque', 'Мечеть', 'Мячэць', 'Mosque', 'bi bi-moon', '#1A6B5E'),
('synagogue', 'Синагога', 'Сінагога', 'Synagogue', 'bi bi-star', '#8B6B1A'),
('manor', 'Дворец', 'Палац', 'Palace', 'bi bi-palette', '#6B1A5B'),
('other', 'Прочее', 'Іншае', 'Other', 'bi bi-three-dots', '#555555');

-- Arch Styles
INSERT INTO arch_styles (slug, name_ru, name_be, name_en) VALUES
('gothic', 'Готика', 'Готыка', 'Gothic'),
('renaissance', 'Ренессанс', 'Рэнесанс', 'Renaissance'),
('baroque', 'Барокко', 'Барока', 'Baroque'),
('classicism', 'Классицизм', 'Класіцызм', 'Classicism'),
('eclecticism', 'Эклектика', 'Эклектыка', 'Eclecticism'),
('modernity', 'Модерн', 'Мадэрн', 'Art Nouveau'),
('constructivism', 'Конструктивизм', 'Канструктывізм', 'Constructivism'),
('wooden', 'Деревянное зодчество', 'Драўлянае дойлідства', 'Wooden Architecture'),
('brick', 'Кирпичный стиль', 'Цагляны стыль', 'Brick Style');

-- Tags
INSERT INTO tags (slug, name_ru, name_be, name_en, color_hex) VALUES
('gdk', 'ВКЛ', 'ВКЛ', 'GDL', '#3B5E3F'),
('ww2', 'Великая Отечественная', 'Вялікая Айчынная', 'WWII', '#8B1A2A'),
('wooden', 'Деревянное зодчество', 'Драўлянае дойлідства', 'Wooden Heritage', '#7A5C1E'),
('defensive', 'Оборонительная', 'Абарончая', 'Defensive', '#2A3A5A'),
('royal', 'Королевская', 'Каралеўская', 'Royal', '#7A1A6B'),
('radzivill', 'Радзивиллы', 'Радзівілы', 'Radziwill', '#4A2A1A'),
('orthodox', 'Православная', 'Праваслаўная', 'Orthodox', '#1A4A7A'),
('catholic', 'Католическая', 'Каталіцкая', 'Catholic', '#1A6B5E'),
('slucak', 'Слуцкое наследие', 'Слуцкая спадчына', 'Slutsk Heritage', '#C68B2A'),
('renaissance', 'Ренессанс', 'Рэнесанс', 'Renaissance', '#5B3A7A');

-- NB: user_roles and the default admin user are created at application startup
--     by Data/DbSeeder.cs via RoleManager / UserManager — not by SQL seeds.

-- ============================================================
--  VIEWS
-- ============================================================

-- Objects list view (without heavy LONGTEXT fields)
CREATE VIEW v_objects_list AS
SELECT
    o.id, o.slug,
    o.name_ru, o.name_be, o.name_en,
    o.short_desc_ru, o.short_desc_be, o.short_desc_en,
    o.main_image_url, o.century_start, o.century_end,
    o.preservation_status, o.rating_avg, o.rating_count,
    o.is_featured, o.status,
    c.slug AS category_slug, c.name_ru AS category_ru, c.icon_class, c.color_hex,
    r.code AS region_code, r.name_ru AS region_ru, r.name_en AS region_en,
    loc.lat, loc.lng
FROM heritage_objects o
JOIN categories c ON c.id = o.category_id
JOIN regions r ON r.id = o.region_id
LEFT JOIN object_locations loc ON loc.object_id = o.id
WHERE o.status = 'published' AND o.is_deleted = FALSE;

-- Objects map view (coordinates only)
CREATE VIEW v_objects_map AS
SELECT
    o.id, o.slug, o.name_ru, o.name_en,
    o.main_image_url,
    c.slug AS category_slug, c.icon_class, c.color_hex,
    loc.lat, loc.lng
FROM heritage_objects o
JOIN categories c ON c.id = o.category_id
JOIN object_locations loc ON loc.object_id = o.id
WHERE o.status = 'published' AND o.is_deleted = FALSE AND loc.lat IS NOT NULL;

-- Top rated objects
CREATE VIEW v_top_rated AS
SELECT * FROM v_objects_list
WHERE rating_count >= 3
ORDER BY rating_avg DESC, rating_count DESC
LIMIT 20;
