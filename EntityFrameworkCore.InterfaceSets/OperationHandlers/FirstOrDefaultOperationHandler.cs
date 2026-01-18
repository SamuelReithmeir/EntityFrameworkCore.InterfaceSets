using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class FirstOrDefaultOperationHandler<TResult> : BaseOperationHandler<TResult>
{
    public override bool CanHandle(string operationName, Expression expression) => operationName == "FirstOrDefault";

    public override async Task<TResult> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken = default)
    {
        foreach (var dbSet in dbSets)
        {
            if (await ExecuteInterfaceExpressionOnDbSetAsync(dbSet, expression, interfaceType,
                    cancellationToken) is { } result)
            {
                return result;
            }
        }

        return default!;
    }

    public override TResult Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        foreach (var dbSet in dbSets)
        {
            if (ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType) is { } result)
            {
                return result;
            }
        }

        return default!;
    }
}