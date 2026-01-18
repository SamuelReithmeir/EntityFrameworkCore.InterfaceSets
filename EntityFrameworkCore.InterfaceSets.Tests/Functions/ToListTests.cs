using EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;
using EntityFrameworkCore.InterfaceSets.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests.Functions;

[TestFixture]
public class ToListTests : InterfaceSetTestBase
{
    
    [Test]
    public void ToList_Unfiltered_IArchivable_ReturnsAllArchivableEntities()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.ToList();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.TotalArchivable));
        Assert.That(result.OfType<Product>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalProducts));
        Assert.That(result.OfType<Order>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalOrders));
        Assert.That(result.OfType<Invoice>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalInvoices));
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public void ToList_Filtered_IArchivable_ReturnsOnlyArchivedEntities()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Where(x => x.IsArchived).ToList();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.ArchivedArchivable));
        Assert.That(result, Has.All.Matches<IArchivable>(x => x.IsArchived));
        AssertSqlWasExecuted("IsArchived");
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public void ToList_Unfiltered_ISoftDeletable_ReturnsAllSoftDeletableEntities()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.ToList();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.TotalSoftDeletable));
        Assert.That(result.OfType<Product>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalProducts));
        Assert.That(result.OfType<Order>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalOrders));
        Assert.That(result.OfType<Customer>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalCustomers));
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public void ToList_Filtered_ISoftDeletable_ReturnsOnlyDeletedEntities()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Where(x => x.IsDeleted).ToList();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.DeletedSoftDeletable));
        Assert.That(result, Has.All.Matches<ISoftDeletable>(x => x.IsDeleted));
        AssertSqlWasExecuted("IsDeleted");
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public void ToList_FilteredByNegation_ISoftDeletable_ReturnsOnlyNonDeletedEntities()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Where(x => !x.IsDeleted).ToList();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.NonDeletedSoftDeletable));
        Assert.That(result, Has.All.Matches<ISoftDeletable>(x => !x.IsDeleted));
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task ToListAsync_Unfiltered_IArchivable_ReturnsAllArchivableEntities()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.ToListAsync();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.TotalArchivable));
        Assert.That(result.OfType<Product>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalProducts));
        Assert.That(result.OfType<Order>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalOrders));
        Assert.That(result.OfType<Invoice>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalInvoices));
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public async Task ToListAsync_Filtered_IArchivable_ReturnsOnlyArchivedEntities()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived).ToListAsync();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.ArchivedArchivable));
        Assert.That(result, Has.All.Matches<IArchivable>(x => x.IsArchived));
        AssertSqlWasExecuted("IsArchived");
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public async Task ToListAsync_Unfiltered_ISoftDeletable_ReturnsAllSoftDeletableEntities()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.ToListAsync();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.TotalSoftDeletable));
        Assert.That(result.OfType<Product>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalProducts));
        Assert.That(result.OfType<Order>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalOrders));
        Assert.That(result.OfType<Customer>().Count(), Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalCustomers));
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public async Task ToListAsync_Filtered_ISoftDeletable_ReturnsOnlyDeletedEntities()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.Where(x => x.IsDeleted).ToListAsync();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.DeletedSoftDeletable));
        Assert.That(result, Has.All.Matches<ISoftDeletable>(x => x.IsDeleted));
        AssertSqlWasExecuted("IsDeleted");
        AssertMultipleSqlQueriesExecuted(3);
    }

    [Test]
    public async Task ToListAsync_FilteredByNegation_ISoftDeletable_ReturnsOnlyNonDeletedEntities()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.Where(x => !x.IsDeleted).ToListAsync();

        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.NonDeletedSoftDeletable));
        Assert.That(result, Has.All.Matches<ISoftDeletable>(x => !x.IsDeleted));
        AssertSqlWasExecuted("IsDeleted");
    }
}
