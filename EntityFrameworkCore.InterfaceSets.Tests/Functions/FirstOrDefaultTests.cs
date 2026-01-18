using EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;
using EntityFrameworkCore.InterfaceSets.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests.Functions;

[TestFixture]
public class FirstOrDefaultTests : InterfaceSetTestBase
{
    [Test]
    public void FirstOrDefault_Unfiltered_IArchivable_ReturnsFirstEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.FirstOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IArchivable>());
        AssertSqlWasExecuted();
    }

    [Test]
    public void FirstOrDefault_Filtered_IArchivable_ReturnsFirstMatchingEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Where(x => x.IsArchived).FirstOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsArchived, Is.True);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public void FirstOrDefault_FilteredNoMatch_IArchivable_ReturnsNull()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Where(x => x.IsArchived && !x.IsArchived).FirstOrDefault();

        Assert.That(result, Is.Null);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public void FirstOrDefault_Unfiltered_ISoftDeletable_ReturnsFirstEntity()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.FirstOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ISoftDeletable>());
        AssertSqlWasExecuted();
    }

    [Test]
    public void FirstOrDefault_Filtered_ISoftDeletable_ReturnsFirstMatchingEntity()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Where(x => x.IsDeleted).FirstOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeleted, Is.True);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public void FirstOrDefault_FilteredByNegation_ISoftDeletable_ReturnsFirstNonDeletedEntity()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Where(x => !x.IsDeleted).FirstOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeleted, Is.False);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public void FirstOrDefault_WithPredicate_IArchivable_ReturnsFirstMatchingEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.FirstOrDefault(x => x.IsArchived);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsArchived, Is.True);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task FirstOrDefaultAsync_Unfiltered_IArchivable_ReturnsFirstEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.FirstOrDefaultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<IArchivable>());
        AssertSqlWasExecuted();
    }

    [Test]
    public async Task FirstOrDefaultAsync_Filtered_IArchivable_ReturnsFirstMatchingEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived).FirstOrDefaultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsArchived, Is.True);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task FirstOrDefaultAsync_FilteredNoMatch_IArchivable_ReturnsNull()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived && !x.IsArchived).FirstOrDefaultAsync();

        Assert.That(result, Is.Null);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task FirstOrDefaultAsync_Unfiltered_ISoftDeletable_ReturnsFirstEntity()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.FirstOrDefaultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.InstanceOf<ISoftDeletable>());
        AssertSqlWasExecuted();
    }

    [Test]
    public async Task FirstOrDefaultAsync_Filtered_ISoftDeletable_ReturnsFirstMatchingEntity()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.Where(x => x.IsDeleted).FirstOrDefaultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeleted, Is.True);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task FirstOrDefaultAsync_FilteredByNegation_ISoftDeletable_ReturnsFirstNonDeletedEntity()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.Where(x => !x.IsDeleted).FirstOrDefaultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeleted, Is.False);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task FirstOrDefaultAsync_WithPredicate_IArchivable_ReturnsFirstMatchingEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.FirstOrDefaultAsync(x => x.IsArchived);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsArchived, Is.True);
        AssertSqlWasExecuted("IsArchived");
    }
}

