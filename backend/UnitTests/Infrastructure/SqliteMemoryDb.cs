using DataAccessLayer.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Infrastructure;

/// <summary>
/// SQLite in-memory on one open connection so schema and data are shared across DbContext instances.
/// Supports real transactions and rollback (unlike EF Core InMemory provider).
/// </summary>
public static class SqliteMemoryDb
{
    /// <summary>
    /// Opens a connection, builds the schema from the current model (<see cref="DatabaseFacade.EnsureCreatedAsync"/>).
    /// Caller must dispose the connection when finished (typically per test or per test class).
    /// </summary>
    public static async Task<SqliteConnection> CreatePreparedConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return connection;
    }

    public static ApplicationDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        return new ApplicationDbContext(options);
    }
}
