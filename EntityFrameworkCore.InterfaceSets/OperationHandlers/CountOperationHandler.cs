using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class CountOperationHandler : BaseOperationHandler<int>
{
    public override bool CanHandle(string operationName, Expression expression) => operationName == "Count";

    public override async Task<int> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken = default)
    {
        var sum = 0;
        foreach (var dbSet in dbSets)
        {
            var count = await ExecuteInterfaceExpressionOnDbSetAsync(dbSet, expression, interfaceType,
                cancellationToken);
            sum += count;
        }
        return sum;
    }

    public override int Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        var sum = 0;
        foreach (var dbSet in dbSets)
        {
            var count = ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType);
            sum += count;
        }

        return sum;
    }
}