using EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;
using EntityFrameworkCore.InterfaceSets.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets.Tests.Functions;

[TestFixture]
public class SingleTests : InterfaceSetTestBase
{
    [Test]
    public void Single_WithSpecificFilter_IArchivable_ReturnsSingleEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var products = interfaceSet.OfType<Product>().Where(x => x.Id == 2);
        var result = products.Single();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(2));
        Assert.That(result.IsArchived, Is.True);
    }

    [Test]
    public void Single_MultipleResults_ThrowsException()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        Assert.Throws<InvalidOperationException>(() => 
            interfaceSet.Where(x => x.IsArchived).Single());
    }

    [Test]
    public void Single_NoResults_ThrowsException()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        Assert.Throws<InvalidOperationException>(() => 
            interfaceSet.Where(x => x.IsArchived && !x.IsArchived).Single());
    }

    [Test]
    public void SingleOrDefault_WithSpecificFilter_IArchivable_ReturnsSingleEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var products = interfaceSet.OfType<Product>().Where(x => x.Id == 2);
        var result = products.SingleOrDefault();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(2));
    }

    [Test]
    public void SingleOrDefault_NoResults_ReturnsNull()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = interfaceSet.Where(x => x.IsArchived && !x.IsArchived).SingleOrDefault();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SingleOrDefault_MultipleResults_ThrowsException()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        Assert.Throws<InvalidOperationException>(() => 
            interfaceSet.Where(x => x.IsArchived).SingleOrDefault());
    }

    [Test]
    public async Task SingleAsync_WithSpecificFilter_IArchivable_ReturnsSingleEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var products = interfaceSet.OfType<Product>().Where(x => x.Id == 2);
        var result = await products.SingleAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(2));
        Assert.That(result.IsArchived, Is.True);
    }

    [Test]
    public void SingleAsync_MultipleResults_ThrowsException()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await interfaceSet.Where(x => x.IsArchived).SingleAsync());
    }

    [Test]
    public void SingleAsync_NoResults_ThrowsException()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await interfaceSet.Where(x => x.IsArchived && !x.IsArchived).SingleAsync());
    }

    [Test]
    public async Task SingleOrDefaultAsync_WithSpecificFilter_IArchivable_ReturnsSingleEntity()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var products = interfaceSet.OfType<Product>().Where(x => x.Id == 2);
        var result = await products.SingleOrDefaultAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(2));
    }

    [Test]
    public async Task SingleOrDefaultAsync_NoResults_ReturnsNull()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        var result = await interfaceSet.Where(x => x.IsArchived && !x.IsArchived).SingleOrDefaultAsync();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SingleOrDefaultAsync_MultipleResults_ThrowsException()
    {
        var interfaceSet = Context.InterfaceSet<IArchivable>();

        Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await interfaceSet.Where(x => x.IsArchived).SingleOrDefaultAsync());
    }
}

