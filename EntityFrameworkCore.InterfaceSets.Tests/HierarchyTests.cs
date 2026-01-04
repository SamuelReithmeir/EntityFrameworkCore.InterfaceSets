using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests;

// Test entity hierarchy
public class BaseDocument : IArchivable
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public class Invoice : BaseDocument
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class Contract : BaseDocument
{
    public string ContractNumber { get; set; } = string.Empty;
}

// DbContext with entity hierarchy
public class HierarchyDbContext : DbContext
{
    public HierarchyDbContext(DbContextOptions<HierarchyDbContext> options) : base(options)
    {
    }

    public DbSet<BaseDocument> BaseDocuments { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<Contract> Contracts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure TPH (Table-Per-Hierarchy) inheritance
        modelBuilder.Entity<BaseDocument>()
            .HasDiscriminator<string>("DocumentType")
            .HasValue<BaseDocument>("Base")
            .HasValue<Invoice>("Invoice")
            .HasValue<Contract>("Contract");
    }
}

public class HierarchyTests
{
    private HierarchyDbContext CreateContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<HierarchyDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new HierarchyDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public void InterfaceSet_DoesNotDuplicateEntitiesInHierarchy()
    {
        // Arrange
        using var context = CreateContext();

        // Add some test data
        var invoice = new Invoice
        {
            Title = "Invoice 1",
            InvoiceNumber = "INV-001",
            Amount = 100,
            IsArchived = false
        };
        var contract = new Contract
        {
            Title = "Contract 1",
            ContractNumber = "CTR-001",
            IsArchived = true
        };

        context.Invoices.Add(invoice);
        context.Contracts.Add(contract);
        context.SaveChanges();

        // Act
        var allArchivableItems = context.InterfaceSet<IArchivable>().ToList();

        // Assert - Should only get 2 items, not more (no duplicates)
        Assert.Equal(2, allArchivableItems.Count);

        // Verify we can find each specific item once
        var invoiceCount = allArchivableItems.Count(i => i is Invoice);
        var contractCount = allArchivableItems.Count(i => i is Contract);

        Assert.Equal(1, invoiceCount);
        Assert.Equal(1, contractCount);
    }

    [Fact]
    public void InterfaceSet_OnlyQueriesRootTypeInHierarchy()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var interfaceSet = context.InterfaceSet<IArchivable>();

        // Assert - Should only include BaseDocument, not derived types
        // (EF will query BaseDocument and get all derived types automatically)
        Assert.Single(interfaceSet.EntityTypes);
        Assert.Contains(typeof(BaseDocument), interfaceSet.EntityTypes);
        Assert.DoesNotContain(typeof(Invoice), interfaceSet.EntityTypes);
        Assert.DoesNotContain(typeof(Contract), interfaceSet.EntityTypes);
    }

    [Fact]
    public void InterfaceSet_WorksCorrectlyWithFiltersOnHierarchy()
    {
        // Arrange
        using var context = CreateContext();

        context.Invoices.AddRange(
            new Invoice { Title = "Invoice 1", IsArchived = true, Amount = 100 },
            new Invoice { Title = "Invoice 2", IsArchived = false, Amount = 200 }
        );
        context.Contracts.AddRange(
            new Contract { Title = "Contract 1", IsArchived = true },
            new Contract { Title = "Contract 2", IsArchived = false }
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
}
