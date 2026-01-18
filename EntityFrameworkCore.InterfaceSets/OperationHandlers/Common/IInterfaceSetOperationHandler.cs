using System.Linq.Expressions;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

public interface IInterfaceSetOperationHandler<TResult>
{
    public bool Handles(string operationName, Expression expression);

    public Task<TResult> HandleAsync(Expression expression, IEnumerable<IQueryable> dbSets,Type interfaceType,
        CancellationToken cancellationToken = default);

    public TResult Handle(Expression expression, IEnumerable<IQueryable> dbSets,Type interfaceType);
}