using EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;
using EntityFrameworkCore.InterfaceSets.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests.Functions;

[TestFixture]
public class AnyTests : InterfaceSetTestBase
{
    [Test]
    public void Any_Unfiltered_IArchivable_ReturnsTrue()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Any();

        Assert.That(result, Is.True);
        AssertSqlWasExecuted();
    }

    [Test]
    public void Any_Filtered_IArchivable_WithMatches_ReturnsTrue()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Where(x => x.IsArchived).Any();

        Assert.That(result, Is.True);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public void Any_Filtered_IArchivable_NoMatches_ReturnsFalse()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Where(x => x.IsArchived && !x.IsArchived).Any();

        Assert.That(result, Is.False);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public void Any_WithPredicate_ISoftDeletable_WithMatches_ReturnsTrue()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Any(x => x.IsDeleted);

        Assert.That(result, Is.True);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public void Any_WithPredicate_ISoftDeletable_NoMatches_ReturnsFalse()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = interfaceSet.Any(x => x.IsDeleted && !x.IsDeleted);

        Assert.That(result, Is.False);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task AnyAsync_Unfiltered_IArchivable_ReturnsTrue()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.AnyAsync();

        Assert.That(result, Is.True);
        AssertSqlWasExecuted();
    }

    [Test]
    public async Task AnyAsync_Filtered_IArchivable_WithMatches_ReturnsTrue()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived).AnyAsync();

        Assert.That(result, Is.True);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task AnyAsync_Filtered_IArchivable_NoMatches_ReturnsFalse()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived && !x.IsArchived).AnyAsync();

        Assert.That(result, Is.False);
        AssertSqlWasExecuted("IsArchived");
    }

    [Test]
    public async Task AnyAsync_WithPredicate_ISoftDeletable_WithMatches_ReturnsTrue()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.AnyAsync(x => x.IsDeleted);

        Assert.That(result, Is.True);
        AssertSqlWasExecuted("IsDeleted");
    }

    [Test]
    public async Task AnyAsync_WithPredicate_ISoftDeletable_NoMatches_ReturnsFalse()
    {
        var interfaceSet = Context.InterfaceSet<ISoftDeletable>();

        var result = await interfaceSet.AnyAsync(x => x.IsDeleted && !x.IsDeleted);

        Assert.That(result, Is.False);
        AssertSqlWasExecuted("IsDeleted");
    }
}

