using Microsoft.EntityFrameworkCore;
using System.Collections;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Internal enumerable that combines multiple DbSets into a single enumeration.
/// This class handles the actual iteration over multiple entity types that implement a common interface.
/// </summary>
/// <typeparam name="TInterface">The interface type that all entities implement.</typeparam>
internal class InterfaceSetEnumerable<TInterface> : IEnumerable<TInterface>, IAsyncEnumerable<TInterface>
    where TInterface : class
{
    private readonly DbContext _context;
    private readonly List<Type> _entityTypes;

    /// <summary>
    /// Initializes a new instance of the InterfaceSetEnumerable class.
    /// </summary>
    /// <param name="context">The DbContext containing the entity types.</param>
    /// <param name="entityTypes">The list of entity types to enumerate over.</param>
    public InterfaceSetEnumerable(DbContext context, List<Type> entityTypes)
    {
        _context = context;
        _entityTypes = entityTypes;
    }

    /// <summary>
    /// Returns an enumerator that iterates through all entities implementing TInterface.
    /// </summary>
    public IEnumerator<TInterface> GetEnumerator()
    {
        foreach (var entityType in _entityTypes)
        {
            var dbSet = EntityTypeDiscovery.GetDbSet(_context, entityType);
            foreach (var entity in dbSet)
            {
                yield return (TInterface)entity;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns an async enumerator that iterates through all entities implementing TInterface.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe.</param>
    public async IAsyncEnumerator<TInterface> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var entityType in _entityTypes)
        {
            var dbSet = EntityTypeDiscovery.GetDbSet(_context, entityType);

            if (dbSet is IAsyncEnumerable<object> asyncEnumerable)
            {
                await foreach (var entity in asyncEnumerable.WithCancellation(cancellationToken))
                {
                    yield return (TInterface)entity;
                }
            }
            else
            {
                foreach (var entity in dbSet)
                {
                    yield return (TInterface)entity;
                }
            }
        }
    }
}
