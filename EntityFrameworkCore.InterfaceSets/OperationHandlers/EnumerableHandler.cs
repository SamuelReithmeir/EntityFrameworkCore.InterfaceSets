using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class EnumerableHandler<TElement> : BaseOperationHandler<IEnumerable<TElement>>
{
    public override bool CanHandle(string operationName, Expression expression)
    {
        return true;
    }

    public override Task<IEnumerable<TElement>> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Async not supported for EnumerableHandler, request an IAsyncEnumerable instead");
    }

    public override IEnumerable<TElement> Execute(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType)
    {
        return dbSets.SelectMany(dbSet => ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType));
    }
}