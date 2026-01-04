using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Linq.Expressions;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Represents a queryable collection of entities that implement a specific interface.
/// This is a read-only wrapper that combines multiple DbSets through UNION operations.
/// </summary>
/// <typeparam name="TInterface">The interface type that entities implement.</typeparam>
public class InterfaceSet<TInterface> : IQueryable<TInterface>, IAsyncEnumerable<TInterface>
    where TInterface : class
{
    private readonly DbContext _context;
    private readonly IQueryable<TInterface> _query;
    private readonly List<Type> _entityTypes;
    private readonly InterfaceSetEnumerable<TInterface> _enumerable;
    private readonly InterfaceSetAsyncQueryProvider<TInterface> _queryProvider;

    /// <summary>
    /// Initializes a new instance of the InterfaceSet class.
    /// </summary>
    /// <param name="context">The DbContext to query from.</param>
    public InterfaceSet(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        // Discover all entity types that implement TInterface
        _entityTypes = EntityTypeDiscovery.FindImplementingEntityTypes<TInterface>(context).ToList();

        if (_entityTypes.Count == 0)
        {
            throw new InvalidOperationException(
                $"No entity types implementing {typeof(TInterface).Name} were found in the DbContext model.");
        }

        // Build enumerable, query provider, and queryable
        _enumerable = BuildEnumerable();
        _queryProvider = new InterfaceSetAsyncQueryProvider<TInterface>(_context, _entityTypes, _enumerable);
        _query = BuildUnionQuery();
    }

    /// <summary>
    /// Gets the entity types that implement this interface.
    /// </summary>
    public IReadOnlyList<Type> EntityTypes => _entityTypes.AsReadOnly();

    /// <summary>
    /// Gets the DbSet for a specific entity type that implements this interface.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The DbSet for the specified entity type.</returns>
    public DbSet<TEntity> GetDbSet<TEntity>() where TEntity : class, TInterface
    {
        var entityType = typeof(TEntity);
        if (!_entityTypes.Contains(entityType))
        {
            throw new InvalidOperationException(
                $"Entity type {entityType.Name} does not implement {typeof(TInterface).Name} or is not configured in the DbContext.");
        }

        return _context.Set<TEntity>();
    }

    private InterfaceSetEnumerable<TInterface> BuildEnumerable()
    {
        return new InterfaceSetEnumerable<TInterface>(_context, _entityTypes);
    }

    private IQueryable<TInterface> BuildUnionQuery()
    {
        // Create a queryable that uses our custom async query provider
        // This enables async LINQ operations like FirstAsync, ToListAsync, etc.
        // We need to create a proper constant expression that represents a queryable source
        var queryable = _enumerable.AsQueryable();
        return new InterfaceSetQueryable<TInterface>(queryable.Expression, _queryProvider);
    }

    #region IQueryable<TInterface> Implementation

    public Type ElementType => _query.ElementType;

    public Expression Expression => _query.Expression;

    public IQueryProvider Provider => _query.Provider;

    public IEnumerator<TInterface> GetEnumerator() => _query.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region IAsyncEnumerable<TInterface> Implementation

    public IAsyncEnumerator<TInterface> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return _enumerable.GetAsyncEnumerator(cancellationToken);
    }

    #endregion
}
