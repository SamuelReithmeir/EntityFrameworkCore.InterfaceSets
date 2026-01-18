using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class CountLongOperationHandler : BaseOperationHandler<long>
{
    public override bool CanHandle(string operationName, Expression expression) => operationName == "LongCount";

    public override async Task<long> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken = default)
    {
        long sum = 0;
        foreach (var dbSet in dbSets)
        {
            var count = await ExecuteInterfaceExpressionOnDbSetAsync(dbSet, expression, interfaceType,
                cancellationToken);
            sum += count;
        }
        return sum;
    }

    public override long Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        long sum = 0;
        foreach (var dbSet in dbSets)
        {
            var count = ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType);
            sum += count;
        }

        return sum;
    }
}