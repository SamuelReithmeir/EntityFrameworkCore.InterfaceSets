using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets;

public static class TypeExtensions
{
    /// <summary>
    /// get all types of the model that implement the specified type
    /// </summary>
    /// <param name="context"></param>
    /// <param name="typeToImplement"></param>
    /// <param name="onlyRootType"></param>
    /// <returns></returns>
    public static List<Type> GetImplementingTypes(this DbContext context, Type typeToImplement,
        bool onlyRootType = true)
    {
        var allTypes = context.Model.GetEntityTypes()
            .Select(x => x.ClrType)
            .Where(typeToImplement.IsAssignableFrom)
            .ToList();

        if (!onlyRootType)
        {
            return allTypes;
        }

        return allTypes.Where(x => allTypes.All(y => y != x.BaseType)).ToList();
    }
}