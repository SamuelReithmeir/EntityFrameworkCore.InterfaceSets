using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Discovers entity types from a DbContext that implement a specific interface.
/// </summary>
internal static class EntityTypeDiscovery
{
    /// <summary>
    /// Finds all entity types in the DbContext model that directly implement the specified interface.
    /// Only returns types that explicitly declare the interface, not types that inherit it from a base class.
    /// </summary>
    /// <typeparam name="TInterface">The interface type to search for.</typeparam>
    /// <param name="context">The DbContext to search.</param>
    /// <returns>A collection of CLR types that are entities and directly implement TInterface.</returns>
    public static IEnumerable<Type> FindImplementingEntityTypes<TInterface>(DbContext context)
        where TInterface : class
    {
        var interfaceType = typeof(TInterface);

        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException($"Type {interfaceType.Name} must be an interface.", nameof(TInterface));
        }

        // Get all entity types that directly implement the interface
        var entityTypes = context.Model.GetEntityTypes()
            .Select(et => et.ClrType)
            .Where(type => type.DirectlyImplementsInterface(interfaceType))
            .ToList();

        return entityTypes;
    }

    /// <summary>
    /// Gets the DbSet for a specific entity type from the DbContext.
    /// </summary>
    /// <param name="context">The DbContext instance.</param>
    /// <param name="entityType">The entity type to get the DbSet for.</param>
    /// <returns>The DbSet as IQueryable.</returns>
    public static IQueryable GetDbSet(DbContext context, Type entityType)
    {
        // Use DbContext.Set<TEntity>() method via reflection
        var setMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
            ?.MakeGenericMethod(entityType);

        if (setMethod == null)
        {
            throw new InvalidOperationException($"Could not find Set<T>() method for entity type {entityType.Name}");
        }

        var dbSet = setMethod.Invoke(context, null);
        return (IQueryable)dbSet!;
    }
}
