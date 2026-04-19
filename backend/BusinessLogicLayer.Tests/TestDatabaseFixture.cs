using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Tests;

/// <summary>
/// Cấu hình dùng cho integration test với MySQL thật.
/// Mỗi test sẽ mở transaction riêng và rollback ở cuối để dữ liệu quay về trạng thái ban đầu.
/// </summary>
public abstract class TestDatabaseFixture : IAsyncLifetime
{
    protected ApplicationDbContext DbContext { get; private set; } = null!;
    private IDbContextTransaction _currentTransaction = null!;

    public async Task InitializeAsync()
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("MySQLConnectionString")
                               ?? Environment.GetEnvironmentVariable("TEST_MYSQL_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Không tìm thấy connection string cho MySQL. Hãy cấu hình ConnectionStrings:MySQLConnectionString hoặc TEST_MYSQL_CONNECTION_STRING.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .Options;

        DbContext = new ApplicationDbContext(options);
        await DbContext.Database.OpenConnectionAsync();
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
        }

        if (DbContext != null)
        {
            await DbContext.Database.CloseConnectionAsync();
            await DbContext.DisposeAsync();
        }
    }

    protected async Task BeginTransactionAsync()
    {
        _currentTransaction = await DbContext.Database.BeginTransactionAsync();
    }

    protected async Task RollbackTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync();
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null!;
        }
    }

    protected void ResetChangeTracker()
    {
        DbContext.ChangeTracker.Clear();
    }

    private static IConfiguration BuildConfiguration()
    {
        var basePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "WebAPI"));

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}