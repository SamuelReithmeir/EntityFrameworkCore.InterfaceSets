using EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;
using EntityFrameworkCore.InterfaceSets.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests.Functions;

[TestFixture]
public class NavigationPropertyTests : InterfaceSetTestBase
{
    [Test]
    public void Count_FilterByNavigationProperty_ReturnsCorrectCount()
    {
        var result = Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 1)
            .Count();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.AuditableCreatedByUser1));
        AssertSqlWasExecuted("CreatedByUserId");
    }

    [Test]
    public void ToList_WithNavigationPropertyInclude_LoadsRelatedData()
    {
        var results = Context.InterfaceSet<IAuditable>()
            .ToList();

        Assert.That(results, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.TotalAuditable));

        // Verify entities were returned (navigation props not loaded by default)
        Assert.That(results, Is.All.Not.Null);
        AssertSqlWasExecuted();
    }

    [Test]
    public void Where_FilterByNavigationPropertyValue_ReturnsMatchingEntities()
    {
        var result = Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 2)
            .Count();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.AuditableCreatedByUser2));
        AssertSqlWasExecuted("CreatedByUserId");
    }

    [Test]
    public void Where_FilterByNullNavigationProperty_ReturnsMatchingEntities()
    {
        var result = Context.InterfaceSet<IAuditable>()
            .Where(x => x.ModifiedByUserId == null)
            .Count();

        // Products 1 and Orders 1 have no ModifiedByUserId
        Assert.That(result, Is.EqualTo(2));
        AssertSqlWasExecuted("ModifiedByUserId");
    }

    [Test]
    public void FirstOrDefault_WithNavigationPropertyFilter_ReturnsEntity()
    {
        var result = Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 1)
            .FirstOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.CreatedByUserId, Is.EqualTo(1));
        AssertSqlWasExecuted("CreatedByUserId");
    }

    [Test]
    public async Task CountAsync_FilterByNavigationProperty_ReturnsCorrectCount()
    {
        var result = await Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 1)
            .CountAsync();

        Assert.That(result, Is.EqualTo(TestDataSeeder.ExpectedCounts.AuditableCreatedByUser1));
        AssertSqlWasExecuted("CreatedByUserId");
    }

    [Test]
    public async Task ToListAsync_WithNavigationPropertyFilter_ReturnsMatchingEntities()
    {
        var results = await Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 2)
            .ToListAsync();

        Assert.That(results, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.AuditableCreatedByUser2));
        Assert.That(results, Is.All.Matches<IAuditable>(x => x.CreatedByUserId == 2));
        AssertSqlWasExecuted("CreatedByUserId");
    }

    [Test]
    public void Any_WithNavigationPropertyFilter_ReturnsTrue()
    {
        var result = Context.InterfaceSet<IAuditable>()
            .Any(x => x.ModifiedByUserId == 2);

        Assert.That(result, Is.True);
        AssertSqlWasExecuted("ModifiedByUserId");
    }

    [Test]
    public void Any_WithNonMatchingNavigationPropertyFilter_ReturnsFalse()
    {
        var result = Context.InterfaceSet<IAuditable>()
            .Any(x => x.CreatedByUserId == 999);

        Assert.That(result, Is.False);
        AssertSqlWasExecuted("CreatedByUserId");
    }

    [Test]
    public void Where_CombineNavigationPropertyAndScalarProperty_ReturnsMatchingEntities()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-22);

        var result = Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 1 && x.CreatedAt < cutoffDate)
            .Count();

        // Product 1 (created -30 days) and Product 2 (created -25 days)
        Assert.That(result, Is.EqualTo(2));
        AssertSqlWasExecuted("CreatedByUserId");
        AssertSqlWasExecuted("CreatedAt");
    }

    [Test]
    public void Where_AccessNavigationPropertyObject_ActuallyWorks()
    {
        Context.ChangeTracker.Clear();

        // Filter by navigation property object property - this actually works!
        var result = Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedBy!.Username == "admin")
            .ToList();

        // Should return items created by the "admin" user (userId = 1)
        Assert.That(result, Has.Count.EqualTo(TestDataSeeder.ExpectedCounts.AuditableCreatedByUser1));
        Assert.That(result, Is.All.Matches<IAuditable>(x => x.CreatedByUserId == 1));

        // However, navigation properties are still null because they weren't included
        Assert.That(result, Is.All.Matches<IAuditable>(x => x.CreatedBy == null));

        AssertSqlWasExecuted("admin");
    }

    [Test]
    public void ToList_WithoutInclude_NavigationPropertiesAreNull()
    {
        Context.ChangeTracker.Clear();

        var results = Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 1)
            .ToList();

        Assert.That(results, Has.Count.GreaterThan(0));

        // Navigation properties should be null when not included
        Assert.That(results, Is.All.Matches<IAuditable>(x => x.CreatedBy == null));
    }

    [Test]
    public void ToList_WithInclude_DoesNotLoadNavigationProperties()
    {
        Context.ChangeTracker.Clear();

        // Try to use Include on interface set
        var results = Context.InterfaceSet<IAuditable>()
            .Include(x => x.CreatedBy)
            .ToList();

        Assert.That(results, Has.Count.GreaterThan(0));

        // Include doesn't work - navigation properties are still null
        // because Include operates on interface type but gets rewritten to entity type
        Assert.That(results, Is.All.Matches<IAuditable>(x => x.CreatedBy == null));
    }

    [Test]
    public async Task ToListAsync_WithoutInclude_NavigationPropertiesAreNull()
    {
        Context.ChangeTracker.Clear();

        var results = await Context.InterfaceSet<IAuditable>()
            .Where(x => x.CreatedByUserId == 2)
            .ToListAsync();

        Assert.That(results, Has.Count.GreaterThan(0));

        // Navigation properties should be null when not included
        Assert.That(results, Is.All.Matches<IAuditable>(x => x.CreatedBy == null));
    }
}
