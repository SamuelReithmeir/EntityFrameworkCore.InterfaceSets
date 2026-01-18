using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Extension methods for DbContext to enable interface-based querying.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Gets an InterfaceSet that allows querying all entities implementing the specified interface.
    /// </summary>
    /// <typeparam name="TInterface">The interface type that entities implement.</typeparam>
    /// <param name="context">The DbContext instance.</param>
    /// <returns>An InterfaceSet instance for querying entities through the interface.</returns>
    public static InterfaceSet<TInterface> InterfaceSet<TInterface>(this DbContext context)
        where TInterface : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        return new InterfaceSet<TInterface>(context);
    }
}

