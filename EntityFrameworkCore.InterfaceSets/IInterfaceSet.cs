namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// wrapper around multiple DbSets for entity types implementing <typeparamref name="TInterface"/> to allow querying via the interface.
/// </summary>
/// <typeparam name="TInterface"></typeparam>
public interface IInterfaceSet<out TInterface> : IQueryable<TInterface>
{
    public IAsyncEnumerable<TInterface> AsAsyncEnumerable() => (IAsyncEnumerable<TInterface>)this;

    public IQueryable<TInterface> AsQueryable() => this;
}