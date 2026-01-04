using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.InterfaceSets.Tests;

/// <summary>
/// Tests to verify that the correct SQL statements are generated for InterfaceSet queries.
/// </summary>
public class SqlGenerationTests
{
    private readonly List<string> _loggedCommands = [];

    private TestDbContext CreateContextWithLogging()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .LogTo(message => _loggedCommands.Add(message), [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private string GetLastExecutedSql()
    {
        // Find the last SQL command that was executed (not just preparing)
        var commandMessages = _loggedCommands
            .Where(log => log.Contains("Executing DbCommand"))
            .ToList();

        return commandMessages.LastOrDefault() ?? string.Empty;
    }

    private List<string> GetAllExecutedSql()
    {
        return _loggedCommands
            .Where(log => log.Contains("CommandExecuted") || log.Contains("Executing DbCommand"))
            .ToList();
    }

    private void ClearLogs()
    {
        _loggedCommands.Clear();
    }

    [Fact]
    public async Task InterfaceSet_FirstAsync_GeneratesCorrectSql()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act
        _ = await context.InterfaceSet<IArchivable>()
            .FirstAsync(x => x.IsArchived);

        // Assert
        var executedSql = GetAllExecutedSql();

        // Should execute separate queries for each entity type
        Assert.NotEmpty(executedSql);

        // Each query should:
        // 1. Query the specific table (Documents or Products)
        // 2. Have a WHERE clause for IsArchived
        // 3. Have a LIMIT clause (for First)

        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));

        Assert.NotNull(documentQuery);
        Assert.Contains("WHERE", documentQuery);
        Assert.Contains("\"IsArchived\"", documentQuery);
        Assert.Contains("LIMIT", documentQuery);

        // Product query might not execute if Document query found a match first
        // This is correct behavior - we should stop after finding first match
    }

    [Fact]
    public async Task InterfaceSet_CountAsync_GeneratesCorrectSql()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = false });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act
        _ = await context.InterfaceSet<IArchivable>()
            .CountAsync(x => x.IsArchived);

        // Assert
        var executedSql = GetAllExecutedSql();

        // Should execute COUNT queries for each entity type
        Assert.NotEmpty(executedSql);

        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));
        var productQuery = executedSql.FirstOrDefault(sql => sql.Contains("Products"));

        // Both queries should execute for count
        Assert.NotNull(documentQuery);
        Assert.NotNull(productQuery);

        // Each should be a COUNT query with WHERE clause
        Assert.Contains("COUNT", documentQuery);
        Assert.Contains("WHERE", documentQuery);
        Assert.Contains("\"IsArchived\"", documentQuery);

        Assert.Contains("COUNT", productQuery);
        Assert.Contains("WHERE", productQuery);
        Assert.Contains("\"IsArchived\"", productQuery);
    }

    [Fact]
    public async Task InterfaceSet_AnyAsync_GeneratesCorrectSql()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = false });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act
        _ = await context.InterfaceSet<IArchivable>()
            .AnyAsync(x => x.IsArchived);

        // Assert
        var executedSql = GetAllExecutedSql();

        Assert.NotEmpty(executedSql);

        // Should generate queries that check existence
        var queries = executedSql.Where(sql => sql.Contains("WHERE")).ToList();
        Assert.NotEmpty(queries);

        // Should have WHERE clause for the predicate
        Assert.All(queries, sql =>
        {
            Assert.Contains("WHERE", sql);
            Assert.Contains("\"IsArchived\"", sql);
        });
    }

    [Fact]
    public async Task InterfaceSet_WithOrderBy_EachTableQueriedSeparately()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        var now = DateTime.Now;
        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true, ArchivedAt = now.AddDays(-2) });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true, ArchivedAt = now.AddDays(-1) });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act
        _ = await context.InterfaceSet<IArchivable>()
            .FirstAsync(x => x.IsArchived);

        // Assert
        var executedSql = GetAllExecutedSql();

        Assert.NotEmpty(executedSql);

        // Should query Documents and/or Products tables separately
        // Cannot do a SQL UNION/JOIN across different entity types
        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));

        Assert.NotNull(documentQuery);
        Assert.Contains("WHERE", documentQuery);
        Assert.Contains("\"IsArchived\"", documentQuery);

        // Should NOT contain UNION (that would be incorrect for different entity types)
        Assert.DoesNotContain("UNION", documentQuery);
    }

    [Fact]
    public async Task InterfaceSet_DoesNotGenerateUnionAcrossEntityTypes()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true });
        context.Orders.Add(new Order { OrderNumber = "ORD1", IsDeleted = true });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act - Query across multiple entity types
        _ = context.InterfaceSet<IArchivable>()
            .Where(x => x.IsArchived)
            .ToList();

        // Assert
        var executedSql = GetAllExecutedSql();

        Assert.NotEmpty(executedSql);

        // Should execute separate queries, NOT a UNION
        Assert.All(executedSql, sql =>
        {
            Assert.DoesNotContain("UNION", sql);
        });

        // Should query Documents and Products separately
        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));
        var productQuery = executedSql.FirstOrDefault(sql => sql.Contains("Products"));
        var orderQuery = executedSql.FirstOrDefault(sql => sql.Contains("Orders"));

        Assert.NotNull(documentQuery);
        Assert.NotNull(productQuery);
        Assert.Null(orderQuery); // Orders don't implement IArchivable
    }

    [Fact]
    public async Task InterfaceSet_SingleAsync_QueriesAllTables()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        // Add only one archived item total across both tables
        context.Documents.Add(new Document { Title = "Doc1", IsArchived = false });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act
        _ = await context.InterfaceSet<IArchivable>()
            .SingleAsync(x => x.IsArchived);

        // Assert
        var executedSql = GetAllExecutedSql();

        Assert.NotEmpty(executedSql);

        // Single must query ALL tables to ensure there's only one match
        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));
        var productQuery = executedSql.FirstOrDefault(sql => sql.Contains("Products"));

        Assert.NotNull(documentQuery);
        Assert.NotNull(productQuery);

        Assert.Contains("WHERE", documentQuery);
        Assert.Contains("\"IsArchived\"", documentQuery);

        Assert.Contains("WHERE", productQuery);
        Assert.Contains("\"IsArchived\"", productQuery);
    }

    [Fact]
    public void InterfaceSet_SynchronousQuery_LoadsFromDatabase()
    {
        // Arrange
        using var context = CreateContextWithLogging();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = false });
        context.SaveChanges();

        ClearLogs();

        // Act - Synchronous query (via enumeration)
        _ = context.InterfaceSet<IArchivable>()
            .Where(x => x.IsArchived)
            .ToList();

        // Assert
        var executedSql = GetAllExecutedSql();

        // Synchronous queries use enumeration which loads all data from each DbSet
        // The queries will execute to load the data from the database
        Assert.NotEmpty(executedSql);

        // Should query each table
        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));
        var productQuery = executedSql.FirstOrDefault(sql => sql.Contains("Products"));

        Assert.NotNull(documentQuery);
        Assert.NotNull(productQuery);

        // Note: The WHERE clause from the LINQ query is applied in-memory after loading data
        // This is a limitation of the current implementation for synchronous queries
        // Async queries (FirstAsync, CountAsync, etc.) do push predicates to the database
    }

    [Fact]
    public async Task InterfaceSet_ComplexPredicate_PredicateAppliedToEachTable()
    {
        // Arrange
        await using var context = CreateContextWithLogging();

        var now = DateTime.Now;
        context.Documents.Add(new Document
        {
            Title = "Doc1",
            IsArchived = true,
            ArchivedAt = now.AddDays(-30)
        });
        context.Products.Add(new Product
        {
            Name = "Product1",
            IsArchived = true,
            ArchivedAt = now.AddDays(-5)
        });
        await context.SaveChangesAsync();

        ClearLogs();

        // Act - Complex predicate with multiple conditions
        var cutoffDate = now.AddDays(-10);
        _ = await context.InterfaceSet<IArchivable>()
            .CountAsync(x => x.IsArchived && x.ArchivedAt < cutoffDate);

        // Assert
        var executedSql = GetAllExecutedSql();

        Assert.NotEmpty(executedSql);

        // Both tables should be queried with the complex predicate
        var documentQuery = executedSql.FirstOrDefault(sql => sql.Contains("Documents"));
        var productQuery = executedSql.FirstOrDefault(sql => sql.Contains("Products"));

        Assert.NotNull(documentQuery);
        Assert.NotNull(productQuery);

        // Each should have both conditions in WHERE clause
        Assert.Contains("WHERE", documentQuery);
        Assert.Contains("\"IsArchived\"", documentQuery);
        Assert.Contains("\"ArchivedAt\"", documentQuery);

        Assert.Contains("WHERE", productQuery);
        Assert.Contains("\"IsArchived\"", productQuery);
        Assert.Contains("\"ArchivedAt\"", productQuery);
    }
}
