using Dapper;
using Microsoft.Data.Sqlite;
using MysteryMud.Core.Persistence;
using MysteryMud.Core.Persistence.Snapshots;
using System.Text.Json;

namespace MysteryMud.Infrastructure.Persistence;

/// <summary>
/// SQLite + Dapper implementation of IPersistenceService.
/// All player data is saved/loaded in a single transaction per player.
/// </summary>
public sealed class SqlitePersistenceService : IPersistenceService
{
    private readonly string _connectionString;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public SqlitePersistenceService(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ─────────────────────────────────────────────────────────
    //  Save
    // ─────────────────────────────────────────────────────────

    public async Task<long> SavePlayerAsync(PlayerSnapshot snap, CancellationToken ct = default)
    {
        var conn = await OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        try
        {
            var playerId = await UpsertPlayerCoreAsync(conn, snap);
            await SaveStatsAsync(conn, playerId, snap.Stats);
            await SaveResourcesAsync(conn, playerId, snap.Resources);
            await SaveIRVAsync(conn, playerId, snap.IRV);
            await SaveEffectsAsync(conn, playerId, snap.Effects);
            await SaveAbilitiesAsync(conn, playerId, snap.Abilities);
            await SaveItemsAsync(conn, playerId, snap.Items);

            await tx.CommitAsync(ct);
            return playerId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<long> UpsertPlayerCoreAsync(SqliteConnection conn, PlayerSnapshot snap)
    {
        const string sql = """
        INSERT INTO players (name, level, location_key, position, form,
                             total_xp, auto_behavior, optional_json, saved_at)
        VALUES (@Name, @Level, @LocationKey, @Position, @Form,
                @TotalXp, @AutoBehavior, @OptionalJson, @SavedAt)
        ON CONFLICT(name) DO UPDATE SET
            level         = excluded.level,
            location_key  = excluded.location_key,
            position      = excluded.position,
            form          = excluded.form,
            total_xp      = excluded.total_xp,
            auto_behavior = excluded.auto_behavior,
            optional_json = excluded.optional_json,
            saved_at      = excluded.saved_at
        RETURNING id;
        """;

        return await conn.ExecuteScalarAsync<long>(sql, new
        {
            snap.Name,
            snap.Level,
            snap.LocationKey,
            snap.Position,
            snap.Form,
            snap.TotalXp,
            snap.AutoBehavior,
            snap.OptionalJson,
            SavedAt = DateTime.UtcNow.ToString("O")
        });
    }

    // Stats — full replace (small fixed set, cheap)
    private static async Task SaveStatsAsync(SqliteConnection conn, long playerId, StatSnapshot[] stats)
    {
        await conn.ExecuteAsync(
            "DELETE FROM player_stats WHERE player_id = @playerId",
            new { playerId });

        if (stats.Length == 0) return;

        await conn.ExecuteAsync("""
        INSERT INTO player_stats (player_id, stat, base_value, eff_value)
        VALUES (@PlayerId, @Stat, @BaseValue, @EffValue)
        """,
            stats.Select(s => new
            {
                PlayerId = playerId,
                s.Stat,
                s.BaseValue,
                s.EffValue
            }));
    }

    // Resources — full replace
    private static async Task SaveResourcesAsync(SqliteConnection conn, long playerId, ResourceSnapshot[] resources)
    {
        await conn.ExecuteAsync(
            "DELETE FROM player_resources WHERE player_id = @playerId",
            new { playerId });

        if (resources.Length == 0) return;

        await conn.ExecuteAsync("""
        INSERT INTO player_resources (player_id, resource, current, maximum, base_regen, current_regen)
        VALUES (@PlayerId, @Resource, @Current, @Maximum, @BaseRegen, @CurrentRegen)
        """,
            resources.Select(r => new
            {
                PlayerId = playerId,
                r.Resource,
                r.Current,
                r.Maximum,
                r.BaseRegen,
                r.CurrentRegen
            }));
    }

    // IRV — full replace
    private static async Task SaveIRVAsync(SqliteConnection conn, long playerId, IRVSnapshot irv)
    {
        await conn.ExecuteAsync(
            "DELETE FROM player_irv WHERE player_id = @playerId",
            new { playerId });

        if (irv == null) return;

        await conn.ExecuteAsync("""
        INSERT INTO player_irv (player_id, base_imm, base_res, base_vuln, eff_imm, eff_res, eff_vuln)
        VALUES (@PlayerId, @BaseImm, @BaseRes, @BaseVuln, @EffImm, @EffRes, @EffVuln)
        """, new
        {
            PlayerId = playerId,
            irv.BaseImm,
            irv.BaseRes,
            irv.BaseVuln,
            irv.EffImm,
            irv.EffRes,
            irv.EffVuln
        });
    }

    // Effects — full replace (effects list changes atomically)
    private static async Task SaveEffectsAsync(SqliteConnection conn, long playerId, EffectSnapshot[] effects)
    {
        await conn.ExecuteAsync(
            "DELETE FROM player_effects WHERE player_id = @playerId",
            new { playerId });

        if (effects.Length == 0) return;

        await conn.ExecuteAsync("""
        INSERT INTO player_effects
            (player_id, effect_id, tags, tick_rate,
             next_tick_offset, expiration_remaining, params_json)
        VALUES
            (@PlayerId, @EffectId, @Tags, @TickRate,
             @NextTickOffset, @ExpirationRemaining, @ParamsJson)
        """,
            effects.Select(e => new
            {
                PlayerId = playerId,
                e.EffectId,
                Tags = JsonSerializer.Serialize(e.Tags, JsonOpts),
                e.TickRate,
                e.NextTickOffset,
                e.ExpirationRemaining,
                e.ParamsJson
            }));
    }

    // Abilities — upsert per (player, ability_key, class_key)
    private static async Task SaveAbilitiesAsync(SqliteConnection conn, long playerId, AbilitySnapshot[] abilities)
    {
        // Remove abilities that are no longer present
        var keys = abilities.Select(a => $"{a.AbilityKey}|{a.ClassKey}").ToHashSet();

        var existing = (await conn.QueryAsync<(string ability_key, string class_key)>(
            "SELECT ability_key, class_key FROM player_abilities WHERE player_id = @playerId",
            new { playerId })).ToList();

        foreach (var (abilityKey, classKey) in existing)
        {
            if (!keys.Contains($"{abilityKey}|{classKey}"))
            {
                await conn.ExecuteAsync("""
                DELETE FROM player_abilities
                WHERE player_id = @playerId AND ability_key = @abilityKey AND class_key = @classKey
                """,
                    new { playerId, abilityKey, classKey });
            }
        }

        if (abilities.Length == 0) return;

        await conn.ExecuteAsync("""
        INSERT INTO player_abilities
            (player_id, ability_key, class_key, learned_percent, learned_level,
             mastery_tier, cooldown_ticks_remaining, charges)
        VALUES
            (@PlayerId, @AbilityKey, @ClassKey, @LearnedPercent, @LearnedLevel,
             @MasteryTier, @CooldownTicksRemaining, @Charges)
        ON CONFLICT(player_id, ability_key, class_key) DO UPDATE SET
            learned_percent          = excluded.learned_percent,
            learned_level            = excluded.learned_level,
            mastery_tier             = excluded.mastery_tier,
            cooldown_ticks_remaining = excluded.cooldown_ticks_remaining,
            charges                  = excluded.charges
        """,
            abilities.Select(a => new
            {
                PlayerId = playerId,
                a.AbilityKey,
                a.ClassKey,
                a.LearnedPercent,
                a.LearnedLevel,
                a.MasteryTier,
                a.CooldownTicksRemaining,
                a.Charges
            }));
    }

    // Items — full replace (simplest correct approach; item lists are per-player)
    private static async Task SaveItemsAsync(SqliteConnection conn, long playerId, ItemSnapshot[] items)
    {
        // Delete old item_effects via cascade, then old items
        await conn.ExecuteAsync(
            "DELETE FROM items WHERE owner_player_id = @playerId",
            new { playerId });

        if (items.Length == 0) return;

        // Insert items first (without container refs), collect new ids
        var idMap = new Dictionary<long, long>(); // old snapshot id → new db id

        foreach (var item in items)
        {
            var newId = await conn.ExecuteScalarAsync<long>("""
            INSERT INTO items (template_vnum, owner_player_id, equipped_slot,
                               container_item_id, params_json)
            VALUES (@TemplateVnum, @OwnerPlayerId, @EquippedSlot, NULL, @ParamsJson)
            RETURNING id;
            """,
                new
                {
                    item.TemplateVnum,
                    OwnerPlayerId = playerId,
                    item.EquippedSlot,
                    item.ParamsJson
                });

            idMap[item.Id] = newId;

            if (item.Effects.Length > 0)
                await SaveItemEffectsAsync(conn, newId, item.Effects);
        }

        // Second pass: wire up container references now that all ids are known
        foreach (var item in items.Where(i => i.ContainerItemId.HasValue))
        {
            if (idMap.TryGetValue(item.ContainerItemId!.Value, out var mappedContainerId))
            {
                await conn.ExecuteAsync("""
                UPDATE items SET container_item_id = @ContainerId WHERE id = @ItemId
                """,
                    new { ContainerId = mappedContainerId, ItemId = idMap[item.Id] });
            }
        }
    }

    private static async Task SaveItemEffectsAsync(SqliteConnection conn, long itemId, EffectSnapshot[] effects)
    {
        await conn.ExecuteAsync("""
        INSERT INTO item_effects
            (item_id, effect_id, tags, tick_rate,
             next_tick_offset, expiration_remaining, params_json)
        VALUES
            (@ItemId, @EffectId, @Tags, @TickRate,
             @NextTickOffset, @ExpirationRemaining, @ParamsJson)
        """,
            effects.Select(e => new
            {
                ItemId = itemId,
                e.EffectId,
                Tags = JsonSerializer.Serialize(e.Tags, JsonOpts),
                e.TickRate,
                e.NextTickOffset,
                e.ExpirationRemaining,
                e.ParamsJson
            }));
    }

    // ─────────────────────────────────────────────────────────
    //  Load
    // ─────────────────────────────────────────────────────────

    public async Task<PlayerSnapshot?> LoadPlayerAsync(string name, CancellationToken ct = default)
    {
        var conn = await OpenConnectionAsync(ct);

        var row = await conn.QuerySingleOrDefaultAsync("""
        SELECT id, name, level, location_key, position, form,
               total_xp, auto_behavior, optional_json
        FROM players WHERE name = @name COLLATE NOCASE
        """, new { name });

        if (row is null) return null;

        long playerId = row.id;

        var stats = (await conn.QueryAsync<StatSnapshot>("""
        SELECT stat, base_value AS BaseValue, eff_value AS EffValue
        FROM player_stats WHERE player_id = @playerId
        """, new { playerId })).ToArray();

        var resources = (await conn.QueryAsync<ResourceSnapshot>("""
        SELECT resource, current, maximum, base_regen AS BaseRegen, current_regen AS CurrentRegen
        FROM player_resources WHERE player_id = @playerId
        """, new { playerId })).ToArray();

        var irv = (await conn.QueryAsync<IRVSnapshot>("""
        SELECT base_imm as BaseImm, base_res AS BaseRes, base_vuln AS BaseVuln, eff_imm AS EffImm, eff_res AS EffRes, eff_vuln AS EffVuln
        FROM player_irv WHERE player_id = @playerId
        """, new { playerId })).Single();

        var effects = await LoadEffectRowsAsync(conn, "player_effects", "player_id", playerId);

        var abilityRows = await conn.QueryAsync("""
        SELECT ability_key, class_key, learned_percent, learned_level,
               mastery_tier, cooldown_ticks_remaining, charges
        FROM player_abilities WHERE player_id = @playerId
        """, new { playerId });

        var abilities = abilityRows.Select(a => new AbilitySnapshot(
            (string)a.ability_key,
            (string)a.class_key,
            (int)a.learned_percent,
            (int)a.learned_level,
            (int)a.mastery_tier,
            (long?)a.cooldown_ticks_remaining,
            (int?)a.charges)).ToArray();

        var items = await LoadItemsAsync(conn, playerId);

        return new PlayerSnapshot(
            Id: playerId,
            Name: (string)row.name,
            Level: (int)row.level,
            LocationKey: (string)row.location_key,
            Position: (string)row.position,
            Form: (string)row.form,
            TotalXp: (long)row.total_xp,
            AutoBehavior: (int)row.auto_behavior,
            OptionalJson: (string?)row.optional_json,
            IRV: irv,
            Stats: stats,
            Resources: resources,
            Effects: effects,
            Abilities: abilities,
            Items: items);
    }

    private static async Task<EffectSnapshot[]> LoadEffectRowsAsync(
        SqliteConnection conn,
        string table,
        string fkColumn,
        long fkValue)
    {
        var rows = await conn.QueryAsync($"""
        SELECT effect_id, tags, tick_rate, next_tick_offset, expiration_remaining, params_json
        FROM {table} WHERE {fkColumn} = @fkValue
        """, new { fkValue });

        return rows.Select(r => new EffectSnapshot(
            EffectId: (string)r.effect_id,
            Tags: JsonSerializer.Deserialize<string[]>((string)r.tags, JsonOpts) ?? [],
            TickRate: (long)r.tick_rate,
            NextTickOffset: (long)r.next_tick_offset,
            ExpirationRemaining: (long)r.expiration_remaining,
            ParamsJson: (string?)r.params_json)).ToArray();
    }

    private static async Task<ItemSnapshot[]> LoadItemsAsync(SqliteConnection conn, long playerId)
    {
        var itemRows = await conn.QueryAsync("""
        SELECT id, template_vnum, equipped_slot, container_item_id, params_json
        FROM items WHERE owner_player_id = @playerId
        """, new { playerId });

        var result = new List<ItemSnapshot>();

        foreach (var row in itemRows)
        {
            long itemId = (long)row.id;
            var effects = await LoadEffectRowsAsync(conn, "item_effects", "item_id", itemId);

            result.Add(new ItemSnapshot(
                Id: itemId,
                TemplateVnum: (int)row.template_vnum,
                EquippedSlot: (string?)row.equipped_slot,
                ContainerItemId: (long?)row.container_item_id,
                ParamsJson: (string?)row.params_json,
                Effects: effects));
        }

        return result.ToArray();
    }

    // ─────────────────────────────────────────────────────────
    //  Misc
    // ─────────────────────────────────────────────────────────

    public async Task DeletePlayerAsync(long playerId, CancellationToken ct = default)
    {
        var conn = await OpenConnectionAsync(ct);
        // Cascades handle stats, resources, irv, effects, abilities, items, item_effects
        await conn.ExecuteAsync(
            "DELETE FROM players WHERE id = @playerId",
            new { playerId });
    }

    public async Task<bool> PlayerExistsAsync(string name, CancellationToken ct = default)
    {
        var conn = await OpenConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(1) FROM players WHERE name = @name COLLATE NOCASE",
            new { name }) > 0;
    }

    //
    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync("PRAGMA foreign_keys = ON;");
        return conn;
    }
}
