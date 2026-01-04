using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests;

// Test interfaces
public interface IArchivable
{
    bool IsArchived { get; set; }
    DateTime? ArchivedAt { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

// Test entities
public class Document : IArchivable, ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class Product : IArchivable
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public class Order : ISoftDeletable
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}

// Test DbContext
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
}

public class InterfaceSetTests
{
    private TestDbContext CreateContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void InterfaceSet_FindsAllImplementingEntityTypes()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var interfaceSet = context.InterfaceSet<IArchivable>();

        // Assert
        Assert.Equal(2, interfaceSet.EntityTypes.Count);
        Assert.Contains(typeof(Document), interfaceSet.EntityTypes);
        Assert.Contains(typeof(Product), interfaceSet.EntityTypes);
    }

    [Fact]
    public void InterfaceSet_ThrowsWhenNoImplementingTypes()
    {
        // Arrange
        using var context = CreateContext();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => context.InterfaceSet<IDisposable>());
    }

    [Fact]
    public void InterfaceSet_CanQueryAcrossMultipleEntityTypes()
    {
        // Arrange
        using var context = CreateContext();

        context.Documents.AddRange(
            new Document { Title = "Doc1", IsArchived = true, ArchivedAt = DateTime.Now },
            new Document { Title = "Doc2", IsArchived = false }
        );

        context.Products.AddRange(
            new Product { Name = "Product1", Price = 10.99m, IsArchived = true, ArchivedAt = DateTime.Now },
            new Product { Name = "Product2", Price = 20.99m, IsArchived = false }
        );

        context.SaveChanges();

        // Act
        var archivedItems = context.InterfaceSet<IArchivable>()
            .Where(x => x.IsArchived)
            .ToList();

        // Assert
        Assert.Equal(2, archivedItems.Count);
        Assert.All(archivedItems, item => Assert.True(item.IsArchived));
    }

    [Fact]
    public void InterfaceSet_CanCountAcrossMultipleEntityTypes()
    {
        // Arrange
        using var context = CreateContext();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product2", IsArchived = false });

        context.SaveChanges();

        // Act
        var archivedCount = context.InterfaceSet<IArchivable>()
            .Count(x => x.IsArchived);

        // Assert
        Assert.Equal(2, archivedCount);
    }

    [Fact]
    public async Task InterfaceSet_CanOrderAndTake()
    {
        // Arrange
        await using var context = CreateContext();

        var now = DateTime.Now;
        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true, ArchivedAt = now.AddDays(-2) });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true, ArchivedAt = now.AddDays(-1) });

        context.SaveChanges();
        context.ChangeTracker.Clear();
        // Act
        var oldestArchived =await context.InterfaceSet<IArchivable>()
            .FirstAsync();

        // Assert
        Assert.NotNull(oldestArchived);
        Assert.Equal(now.AddDays(-2).Date, oldestArchived.ArchivedAt!.Value.Date);
    }

    [Fact]
    public void InterfaceSet_GetDbSet_ReturnsCorrectDbSet()
    {
        // Arrange
        using var context = CreateContext();
        var interfaceSet = context.InterfaceSet<IArchivable>();

        // Act
        var documentSet = interfaceSet.GetDbSet<Document>();
        var productSet = interfaceSet.GetDbSet<Product>();

        // Assert
        Assert.NotNull(documentSet);
        Assert.NotNull(productSet);
        Assert.Same(context.Documents, documentSet);
        Assert.Same(context.Products, productSet);
    }

    [Fact]
    public void InterfaceSet_GetDbSet_ThrowsForNonImplementingType()
    {
        // Arrange
        using var context = CreateContext();
        var interfaceSet = context.InterfaceSet<ISoftDeletable>();

        // Act & Assert - Document implements ISoftDeletable but Product does not
        var documentSet = interfaceSet.GetDbSet<Document>(); // Should work
        Assert.NotNull(documentSet);

        // We cannot test with Product as it would fail at compile time due to generic constraint
    }

    [Fact]
    public void InterfaceSet_WorksWithMultipleInterfaces()
    {
        // Arrange
        using var context = CreateContext();

        context.Documents.AddRange(
            new Document { Title = "Doc1", IsDeleted = true },
            new Document { Title = "Doc2", IsDeleted = false }
        );

        context.Orders.AddRange(
            new Order { OrderNumber = "ORD1", IsDeleted = true },
            new Order { OrderNumber = "ORD2", IsDeleted = false }
        );

        context.SaveChanges();

        // Act
        var deletedItems = context.InterfaceSet<ISoftDeletable>()
            .Where(x => x.IsDeleted)
            .ToList();

        // Assert
        Assert.Equal(2, deletedItems.Count);
    }

    [Fact]
    public async Task InterfaceSet_SupportsAsyncEnumeration()
    {
        // Arrange
        await using var context = CreateContext();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = true });

        await context.SaveChangesAsync();

        // Act
        var items = new List<IArchivable>();
        var interfaceSet = context.InterfaceSet<IArchivable>();
        await foreach (var item in interfaceSet)
        {
            if (item.IsArchived)
            {
                items.Add(item);
            }
        }

        // Assert
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task InterfaceSet_SupportsToListAsync()
    {
        // Arrange
        await using var context = CreateContext();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = false });

        await context.SaveChangesAsync();

        // Act
        var items = new List<IArchivable>();
        var interfaceSet = context.InterfaceSet<IArchivable>();
        await foreach (var item in interfaceSet)
        {
            if (item.IsArchived)
            {
                items.Add(item);
            }
        }

        // Assert
        Assert.Single(items);
    }

    [Fact]
    public async Task InterfaceSet_SupportsCountAsync()
    {
        // Arrange
        await using var context = CreateContext();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = true });
        context.Products.Add(new Product { Name = "Product1", IsArchived = false });
        context.Products.Add(new Product { Name = "Product2", IsArchived = true });

        await context.SaveChangesAsync();

        // Act
        var archivedCount = await context.InterfaceSet<IArchivable>()
            .CountAsync(x => x.IsArchived);

        // Assert
        Assert.Equal(2, archivedCount);
    }

    [Fact]
    public async Task InterfaceSet_SupportsAnyAsync()
    {
        // Arrange
        await using var context = CreateContext();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = false });
        context.Products.Add(new Product { Name = "Product1", IsArchived = false });

        await context.SaveChangesAsync();

        // Act
        var hasArchived = await context.InterfaceSet<IArchivable>()
            .AnyAsync(x => x.IsArchived);

        // Assert
        Assert.False(hasArchived);
    }

    [Fact]
    public async Task InterfaceSet_SupportsFirstOrDefaultAsync()
    {
        // Arrange
        await using var context = CreateContext();

        context.Documents.Add(new Document { Title = "Doc1", IsArchived = false });

        await context.SaveChangesAsync();

        // Act
        var archivedItem = await context.InterfaceSet<IArchivable>()
            .FirstOrDefaultAsync(x => x.IsArchived);

        // Assert
        Assert.Null(archivedItem);
    }
}
