using System.Linq.Expressions;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

public interface IOperationHandler<TResult>
{
    bool CanHandle(string operationName, Expression expression);

    TResult Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType);

    Task<TResult> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType,
        CancellationToken cancellationToken = default);
}
