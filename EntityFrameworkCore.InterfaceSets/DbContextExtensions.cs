using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Extension methods for DbContext to support querying entities by interface.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Returns an InterfaceSet that can be used to query entities implementing the specified interface.
    /// This method automatically discovers all entity types in the DbContext that implement TInterface
    /// and combines them into a single queryable collection.
    /// </summary>
    /// <typeparam name="TInterface">The interface type that entities implement.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <returns>An InterfaceSet representing a queryable union of all entities implementing TInterface.</returns>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no entity types implementing TInterface are found in the DbContext model.
    /// </exception>
    /// <example>
    /// <code>
    /// public interface IArchivable
    /// {
    ///     bool IsArchived { get; set; }
    /// }
    ///
    /// // Query all entities that implement IArchivable
    /// var archivedItems = context.InterfaceSet[IArchivable]()
    ///     .Where(x => x.IsArchived)
    ///     .ToList();
    /// </code>
    /// </example>
    public static InterfaceSet<TInterface> InterfaceSet<TInterface>(this DbContext context)
        where TInterface : class
    {
        return new InterfaceSet<TInterface>(context);
    }
}
