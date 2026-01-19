using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;

public abstract class InterfaceSetTestBase
{
    protected TestDbContext Context { get; private set; } = null!;
    protected SqlCommandLoggerProvider LoggerProvider { get; private set; } = null!;
    protected SqlCommandLogger SqlLogger => LoggerProvider.Logger;

    [SetUp]
    public void SetUp()
    {
        LoggerProvider = new SqlCommandLoggerProvider();
        Context = CreateContext();
        Context.Database.EnsureCreated();
        TestDataSeeder.SeedData(Context);
        SqlLogger.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        Context?.Database.EnsureDeleted();
        Context?.Dispose();
        LoggerProvider?.Dispose();
    }

    protected virtual TestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("DataSource=:memory:")
            .LogTo(
                (eventId, logLevel) => logLevel >= LogLevel.Information,
                eventData => SqlLogger.Log(
                    eventData.LogLevel,
                    eventData.EventId,
                    eventData,
                    null,
                    (state, ex) => state.ToString() ?? string.Empty))
            .EnableSensitiveDataLogging()
            .Options;

        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        return context;
    }

    protected void AssertSqlWasExecuted(string? expectedSqlFragment = null)
    {
        var commands = SqlLogger.ExecutedCommands;
        Assert.That(commands, Is.Not.Empty, "Expected SQL commands to be executed");

        if (expectedSqlFragment != null)
        {
            Assert.That(commands, Has.Some.Contains(expectedSqlFragment),
                $"Expected to find SQL fragment: {expectedSqlFragment}");
        }
    }

    protected void AssertMultipleSqlQueriesExecuted(int expectedCount)
    {
        var commands = SqlLogger.ExecutedCommands;
        Assert.That(commands.Count, Is.GreaterThanOrEqualTo(expectedCount),
            $"Expected at least {expectedCount} SQL commands");
    }

    protected IReadOnlyList<string> GetExecutedSqlCommands() => SqlLogger.ExecutedCommands;

    protected void PrintExecutedSql()
    {
        Console.WriteLine("=== Executed SQL Commands ===");
        foreach (var cmd in SqlLogger.ExecutedCommands)
        {
            Console.WriteLine(cmd);
            Console.WriteLine("---");
        }
    }
}

