using EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;
using EntityFrameworkCore.InterfaceSets.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests.Functions;

[TestFixture]
public class CountTests : InterfaceSetTestBase
{
    [Test]
    public void Count_Unfiltered_IArchivable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Count();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalArchivable));
        AssertMultipleSqlQueriesExecuted(3);
        AssertSqlWasExecuted("COUNT");
    }

    [Test]
    public void Count_Filtered_IArchivable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Count(x => x.IsArchived);

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.ArchivedArchivable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public void Count_Unfiltered_ISoftDeletable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Count();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalSoftDeletable));
        AssertMultipleSqlQueriesExecuted(3);
        AssertSqlWasExecuted("COUNT");
    }

    [Test]
    public void Count_Filtered_ISoftDeletable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Where(x => x.IsDeleted).Count();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.DeletedSoftDeletable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public void Count_FilteredByNegation_ISoftDeletable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Where(x => !x.IsDeleted).Count();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.NonDeletedSoftDeletable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public void Count_FilteredWithPredicate_IArchivable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Count(x => !x.IsArchived);

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.NonArchivedArchivable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task CountAsync_Unfiltered_IArchivable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.CountAsync();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalArchivable));
        AssertMultipleSqlQueriesExecuted(3);
        AssertSqlWasExecuted("COUNT");
    }

    [Test]
    public async Task CountAsync_Filtered_IArchivable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived).CountAsync();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.ArchivedArchivable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task CountAsync_Unfiltered_ISoftDeletable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.CountAsync();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.TotalSoftDeletable));
        AssertMultipleSqlQueriesExecuted(3);
        AssertSqlWasExecuted("COUNT");
    }

    [Test]
    public async Task CountAsync_Filtered_ISoftDeletable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.Where(x => x.IsDeleted).CountAsync();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.DeletedSoftDeletable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task CountAsync_FilteredByNegation_ISoftDeletable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.Where(x => !x.IsDeleted).CountAsync();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.NonDeletedSoftDeletable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task CountAsync_FilteredWithPredicate_IArchivable_ReturnsCorrectCount()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.CountAsync(x => !x.IsArchived);

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.NonArchivedArchivable));
        AssertSqlWasExecuted("COUNT");
        AssertSqlWasExecuted("IsArchived");
    }
}

