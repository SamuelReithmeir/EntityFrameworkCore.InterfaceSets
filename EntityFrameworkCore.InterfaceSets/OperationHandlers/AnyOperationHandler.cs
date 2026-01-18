using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class AnyOperationHandler : BaseOperationHandler<bool>
{
    public override bool CanHandle(string operationName, Expression expression) => operationName == "Any";

    public override async Task<bool> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken = default)
    {
        foreach (var dbSet in dbSets)
        {
            if (await ExecuteInterfaceExpressionOnDbSetAsync(dbSet, expression, interfaceType,
                    cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    public override bool Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        foreach (var dbSet in dbSets)
        {
            if (ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType))
            {
                return true;
            }
        }

        return false;
    }
}