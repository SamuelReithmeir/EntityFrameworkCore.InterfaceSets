namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Extension methods for Type operations.
/// </summary>
internal static class TypeExtensions
{
    /// <summary>
    /// Determines whether the type directly implements the specified interface.
    /// Unlike IsAssignableFrom, this only returns true if the type itself declares the interface,
    /// not if it inherits it from a base class.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="interfaceType">The interface type to check for.</param>
    /// <returns>True if the type directly implements the interface; otherwise, false.</returns>
    public static bool DirectlyImplementsInterface(this Type type, Type interfaceType)
    {
        if (!interfaceType.IsInterface)
        {
            return false;
        }

        // GetInterfaces() returns all interfaces implemented by the type,
        // including those inherited from base classes.
        // We need to check if this specific type declares the interface.
        return type.GetInterfaces().Contains(interfaceType) &&
               (type.BaseType == null || !type.BaseType.GetInterfaces().Contains(interfaceType));
    }
}
