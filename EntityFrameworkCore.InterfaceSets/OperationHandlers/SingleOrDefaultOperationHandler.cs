using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class SingleOrDefaultOperationHandler<TResult> : BaseOperationHandler<TResult>
{
    public override bool CanHandle(string operationName, Expression expression) => operationName == "SingleOrDefault";

    public override async Task<TResult> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken = default)
    {
        TResult? result = default;
        foreach (var dbSet in dbSets)
        {
            if (await ExecuteInterfaceExpressionOnDbSetAsync(dbSet, expression, interfaceType,
                    cancellationToken) is { } single)
            {
                if (result is not null)
                {
                    throw new InvalidOperationException("Sequence contains more than one element");
                }

                result = single;
            }
        }

        return result!;
    }

    public override TResult Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        TResult? result = default;
        foreach (var dbSet in dbSets)
        {
            if (ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType) is { } single)
            {
                if (result is not null)
                {
                    throw new InvalidOperationException("Sequence contains more than one element");
                }

                result = single;
            }
        }

        return result!;
    }
}