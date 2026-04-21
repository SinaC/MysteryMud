using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace MysteryMud.Infrastructure.Persistence.Schema;

// ─────────────────────────────────────────────────────────────
//  Migration contract
// ─────────────────────────────────────────────────────────────

/// <summary>
/// A single versioned migration step.
/// Migrations are discovered automatically from embedded SQL files
/// named  VN__description.sql  (e.g. V2__item_durability.sql).
/// </summary>
public sealed record Migration(
    int Version,
    string Description,
    string Sql);

// ─────────────────────────────────────────────────────────────
//  MigrationRunner
// ─────────────────────────────────────────────────────────────

/// <summary>
/// Applies pending migrations in version order inside a single transaction.
/// Safe to call on every boot — already-applied migrations are skipped.
///
/// Migration files must be embedded resources named:
///     VN__description.sql    (N is a positive integer)
/// e.g.:
///     V1__baseline.sql
///     V2__item_durability_player_title.sql
/// </summary>
public sealed class MigrationRunner
{
    private readonly string _connectionString;
    private readonly ILogger _log;

    public MigrationRunner(string connectionString, ILogger log)
    {
        _connectionString = connectionString;
        _log = log;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Must run outside any transaction — SQLite restriction
        await conn.ExecuteAsync("PRAGMA journal_mode = WAL;");
        await conn.ExecuteAsync("PRAGMA foreign_keys = ON;");

        await EnsureMigrationTableAsync(conn);

        var applied = await GetAppliedVersionsAsync(conn);
        var pending = DiscoverMigrations()
            .Where(m => !applied.Contains(m.Version))
            .OrderBy(m => m.Version)
            .ToList();

        if (pending.Count == 0)
        {
            _log.LogInformation("Persistence: schema is up to date (version {Version})",
                applied.Count > 0 ? applied.Max() : 0);
            return;
        }

        _log.LogInformation("Persistence: applying {Count} pending migration(s)", pending.Count);

        foreach (var migration in pending)
        {
            await using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                _log.LogInformation("Persistence: applying V{Version} — {Description}",
                    migration.Version, migration.Description);

                // Execute each statement in the migration file individually
                foreach (var statement in SplitStatements(migration.Sql))
                    await conn.ExecuteAsync(statement, transaction: (SqliteTransaction)tx);

                await conn.ExecuteAsync("""
                    INSERT INTO schema_migrations (version, description, applied_at)
                    VALUES (@Version, @Description, @AppliedAt)
                    """,
                    new
                    {
                        migration.Version,
                        migration.Description,
                        AppliedAt = DateTime.UtcNow.ToString("O")
                    },
                    transaction: (SqliteTransaction)tx);

                await tx.CommitAsync(ct);

                _log.LogInformation("Persistence: V{Version} applied successfully",
                    migration.Version);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                _log.LogError(ex,
                    "Persistence: migration V{Version} failed — rolling back", migration.Version);
                throw; // Abort startup; do not run further migrations
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Discovery — embedded SQL resources
    // ─────────────────────────────────────────────────────────

    private static readonly Regex MigrationNamePattern =
        new(@"V(\d+)__(.+)\.sql$", RegexOptions.IgnoreCase);

    private static IEnumerable<Migration> DiscoverMigrations()
    {
        var asm = Assembly.GetExecutingAssembly();

        foreach (var resourceName in asm.GetManifestResourceNames())
        {
            var match = MigrationNamePattern.Match(resourceName);
            if (!match.Success) continue;

            var version = int.Parse(match.Groups[1].Value);
            var description = match.Groups[2].Value.Replace('_', ' ');

            using var stream = asm.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            yield return new Migration(version, description, sql);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private static async Task EnsureMigrationTableAsync(SqliteConnection conn)
    {
        await conn.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version     INTEGER PRIMARY KEY,
                description TEXT    NOT NULL,
                applied_at  TEXT    NOT NULL
            )
            """);
    }

    private static async Task<HashSet<int>> GetAppliedVersionsAsync(SqliteConnection conn)
    {
        var versions = await conn.QueryAsync<int>("SELECT version FROM schema_migrations");
        return versions.ToHashSet();
    }

    private static IEnumerable<string> SplitStatements(string sql)
        => sql.Split(';', StringSplitOptions.RemoveEmptyEntries)
              .Select(s => s.Trim())
              .Where(s => !string.IsNullOrWhiteSpace(s));
}