
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets;

public class InterfaceSet<TInterface> : InterfaceSetQueryable<TInterface,TInterface>,IEntityRootExpressionProvider
    where TInterface : class
{
    private readonly DbContext _context;
    internal InterfaceSet(DbContext context) : base(context,typeof(TInterface))
    {
        _context = context;
    }


    public Expression GetEntityRootExpression(Type entityType)
    {
        var setMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
            ?.MakeGenericMethod(entityType);

        if (setMethod == null)
        {
            throw new InvalidOperationException($"Could not find Set<T>() method for entity type {entityType.Name}");
        }

        var dbSet = setMethod.Invoke(_context, null);
        return ((IQueryable)dbSet!).Expression;
    }
}