using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

public abstract class BaseOperationHandler<TResult>: IOperationHandler<TResult>
{
    public abstract bool CanHandle(string operationName, Expression expression);

    public abstract Task<TResult> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,Type interfaceType,
        CancellationToken cancellationToken = default);

    public abstract TResult Execute(Expression expression, IEnumerable<IQueryable> dbSets,Type interfaceType);

    protected TResult ExecuteInterfaceExpressionOnDbSet(IQueryable dbSet, Expression expression,Type interfaceType)
    {
        // Rewrite the expression to target the concrete entity type
        var entityType = dbSet.ElementType;
        var rewriter = new InterfaceEntityExpressionRewriter(interfaceType, entityType);
        var rewrittenExpression = rewriter.Visit(expression);

        // Map TResult to entity result type (e.g., IArchivable -> Order, IEnumerable<IArchivable> -> IEnumerable<Order>)
        var entityResultType = MapInterfaceTypeToEntityType(typeof(TResult), interfaceType, entityType);

        // Execute with entity type
        var executeMethod = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])
            ?.MakeGenericMethod(entityResultType);

        var result = executeMethod?.Invoke(dbSet.Provider, [rewrittenExpression]);

        // Cast back to TResult (entity types implement the interface)
        return (TResult)result!;
    }

    protected Task<TResult> ExecuteInterfaceExpressionOnDbSetAsync(IQueryable dbSet, Expression expression,Type interfaceType,
        CancellationToken cancellationToken = default)
    {
        // Rewrite the expression to target the concrete entity type
        var entityType = dbSet.ElementType;
        var rewriter = new InterfaceEntityExpressionRewriter(interfaceType, entityType);
        var rewrittenExpression = rewriter.Visit(expression);

        // Ensure the provider supports async execution
        if (dbSet.Provider is not IAsyncQueryProvider asyncProvider)
        {
            throw new InvalidOperationException("Underlying provider does not support async execution.");
        }

        // Map TResult to entity result type (e.g., IArchivable -> Order, IEnumerable<IArchivable> -> IEnumerable<Order>)
        var entityResultType = MapInterfaceTypeToEntityType(typeof(TResult), interfaceType, entityType);

        // Execute with entity type, wrapped in Task<>
        var taskEntityResultType = typeof(Task<>).MakeGenericType(entityResultType);

        var executeMethod = typeof(IAsyncQueryProvider)
            .GetMethod(nameof(IAsyncQueryProvider.ExecuteAsync), 1, [typeof(Expression), typeof(CancellationToken)])
            ?.MakeGenericMethod(taskEntityResultType);

        var taskResult = executeMethod?.Invoke(asyncProvider, [rewrittenExpression, cancellationToken]);

        // Convert Task<EntityType> to Task<TResult>
        return ConvertTaskResult((dynamic)taskResult!);
    }

    private static Type MapInterfaceTypeToEntityType(Type resultType, Type interfaceType, Type entityType)
    {
        // If TResult is exactly the interface type, return entity type
        if (resultType == interfaceType)
            return entityType;

        // If TResult is a generic type containing the interface type (e.g., IEnumerable<IArchivable>)
        if (resultType.IsGenericType)
        {
            var genericDefinition = resultType.GetGenericTypeDefinition();
            var typeArgs = resultType.GetGenericArguments();

            // Recursively map type arguments
            var mappedTypeArgs = typeArgs.Select(t => MapInterfaceTypeToEntityType(t, interfaceType, entityType)).ToArray();

            return genericDefinition.MakeGenericType(mappedTypeArgs);
        }

        // Otherwise return as-is (e.g., int, long, etc.)
        return resultType;
    }

    private static async Task<TResult> ConvertTaskResult<TEntity>(Task<TEntity> entityTask)
    {
        var result = await entityTask;
        return (TResult)(object)result!;
    }
}