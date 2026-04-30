-- ============================================================
--  MUD Persistence Schema
--  All tick-relative values are stored as offsets from save time.
--  Permanent durations use -1 as sentinel.
-- ============================================================

-- ------------------------------------------------------------
--  Players
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS players (
    id              INTEGER PRIMARY KEY,
    name            TEXT    NOT NULL UNIQUE COLLATE NOCASE,
    level           INTEGER NOT NULL,
    location_key    TEXT    NOT NULL,   -- "zone_id::room_vnum"
    position        TEXT    NOT NULL,   -- enum name: Standing, Sitting, …
    form            TEXT    NOT NULL,   -- Humanoid, Cat, Bear
    total_xp        INTEGER NOT NULL,
    auto_behavior   INTEGER NOT NULL,   -- bitmask
    optional_json   TEXT,               -- Gender, RespawnState, CommandThrottle, …
    saved_at        TEXT    NOT NULL    -- ISO-8601 UTC
);

-- ------------------------------------------------------------
--  Stats  (base + effective, one row per stat per player)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS player_stats (
    player_id   INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    stat        TEXT    NOT NULL,   -- "Str", "Int", …
    base_value  INTEGER NOT NULL,
    eff_value   INTEGER NOT NULL,
    PRIMARY KEY (player_id, stat)
);

-- ------------------------------------------------------------
--  Resources  (Health / Mana / Energy / Rage)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS player_resources (
    player_id       INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    resource        TEXT    NOT NULL,   -- "Health", "Mana", "Energy", "Rage"
    current         INTEGER NOT NULL,
    maximum         INTEGER NOT NULL,
    base_regen      REAL    NOT NULL,
    current_regen   REAL    NOT NULL,
    PRIMARY KEY (player_id, resource)
);

-- ------------------------------------------------------------
--  IRV  (Immunity / Resistance / Vulnerability)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS player_irv (
    player_id       INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    base_imm        INTEGER NOT NULL,
    base_res        INTEGER NOT NULL,
    base_vuln       INTEGER NOT NULL,
    eff_imm         INTEGER NOT NULL,
    eff_res         INTEGER NOT NULL,
    eff_vuln        INTEGER NOT NULL,
    PRIMARY KEY (player_id)
);

-- ------------------------------------------------------------
--  Effects  (tick values stored as remaining offsets)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS player_effects (
    id                      INTEGER PRIMARY KEY,
    player_id               INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    effect_id               TEXT    NOT NULL,
    tags                    TEXT    NOT NULL,   -- JSON array of strings
    tick_rate               INTEGER NOT NULL,
    next_tick_offset        INTEGER NOT NULL,   -- ticks from load until next application
    expiration_remaining    INTEGER NOT NULL,   -- ticks remaining, -1 = permanent
    params_json             TEXT
);

CREATE INDEX IF NOT EXISTS idx_player_effects_player ON player_effects(player_id);

-- ------------------------------------------------------------
--  Abilities
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS player_abilities (
    id                      INTEGER PRIMARY KEY,
    player_id               INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    ability_key             TEXT    NOT NULL,
    class_key               TEXT    NOT NULL,
    learned_percent         INTEGER NOT NULL,
    learned_level           INTEGER NOT NULL,
    mastery_tier            INTEGER NOT NULL DEFAULT 0,
    cooldown_ticks_remaining INTEGER,          -- NULL if not on cooldown
    charges                 INTEGER,           -- NULL if not charge-based
    UNIQUE (player_id, ability_key, class_key)
);

CREATE INDEX IF NOT EXISTS idx_player_abilities_player ON player_abilities(player_id);

-- ------------------------------------------------------------
--  Items
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS items (
    id                  INTEGER PRIMARY KEY,
    template_vnum       INTEGER NOT NULL,
    owner_player_id     INTEGER REFERENCES players(id) ON DELETE CASCADE,
    equipped_slot       TEXT,               -- NULL if not equipped
    container_item_id   INTEGER REFERENCES items(id) ON DELETE SET NULL,
    params_json         TEXT                -- weapon dice overrides, charges, …
);

CREATE INDEX IF NOT EXISTS idx_items_owner ON items(owner_player_id);

-- ------------------------------------------------------------
--  Item Effects
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS item_effects (
    id                      INTEGER PRIMARY KEY,
    item_id                 INTEGER NOT NULL REFERENCES items(id) ON DELETE CASCADE,
    effect_id               TEXT    NOT NULL,
    tags                    TEXT    NOT NULL,
    tick_rate               INTEGER NOT NULL,
    next_tick_offset        INTEGER NOT NULL,
    expiration_remaining    INTEGER NOT NULL,
    params_json             TEXT
);

CREATE INDEX IF NOT EXISTS idx_item_effects_item ON item_effects(item_id);

-- ------------------------------------------------------------
--  Pets  (future — placeholder, not yet wired)
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS player_pets (
    id          INTEGER PRIMARY KEY,
    player_id   INTEGER NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    npc_vnum    INTEGER NOT NULL,
    name        TEXT,
    level       INTEGER NOT NULL,
    stats_json  TEXT,
    saved_at    TEXT    NOT NULL
);