using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class AsyncEnumerableHandler<TElement> : BaseOperationHandler<IAsyncEnumerable<TElement>>
{
    public override bool CanHandle(string operationName, Expression expression)
    {
        return true;
    }

    public override Task<IAsyncEnumerable<TElement>> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Task<IAsyncEnumerable<TElement>> not supported, request an IAsyncEnumerable<TElement> instead");
    }

    public override async IAsyncEnumerable<TElement> Execute(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType)
    {
        foreach (var dbSet in dbSets)
        {
            await foreach (var element in ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType))
            {
                yield return element;
            }
        }
    }
}