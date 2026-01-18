using System.Linq.Expressions;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.InterfaceSets;

public class InterfaceSetQueryProvider<TInterface> : IAsyncQueryProvider
{
    private readonly DbContext _context;
    private readonly Type _interfaceType;
    private readonly List<Type> _entityTypes;

    public InterfaceSetQueryProvider(DbContext context, Type interfaceType)
    {
        _context = context;
        _interfaceType = interfaceType;
        _entityTypes = context.GetImplementingTypes(interfaceType);
    }

    public IQueryable CreateQuery(Expression expression)
    {
        throw new NotSupportedException(
            "Non-generic CreateQuery is not supported. Use the generic version.");
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new InterfaceSetQueryable<TInterface, TElement>(expression, this);
    }

    public object? Execute(Expression expression)
    {
        throw new NotSupportedException(
            "Non-generic Execute is not supported. Use the generic version.");
    }

    public TResult Execute<TResult>(Expression expression)
    {
        var operationName = GetMethodName(expression);
        var handler = OperationHandlerRegistry.GetHandler<TResult>(operationName, expression);
        return handler.Execute(expression, GetDbSets(expression), _interfaceType);
    }


    public TResult ExecuteAsync<TResult>(Expression expression,
        CancellationToken cancellationToken = default)
    {
        //check if TResult is Task<TActualResult> and then call ExecuteAsyncInternal<TActualResult>
        var resultType = typeof(TResult);

        if (!resultType.IsGenericType || resultType.GetGenericTypeDefinition() != typeof(Task<>))
        {
            //execute sync
            return Execute<TResult>(expression);
        }
        var actualResultType = resultType.GetGenericArguments()[0];
        var method = typeof(InterfaceSetQueryProvider<TInterface>)
            .GetMethod(nameof(ExecuteAsyncInternal), [typeof(Expression), typeof(CancellationToken)])
            ?.MakeGenericMethod(actualResultType);
            
        if (method == null)
        {
            throw new InvalidOperationException($"Could not find ExecuteAsyncInternal method for result type {actualResultType.Name}");
        }
            
        return (TResult)method.Invoke(this, [expression, cancellationToken])!;
    }

    public Task<TResult> ExecuteAsyncInternal<TResult>(Expression expression,
        CancellationToken cancellationToken = default)
    {
        var operationName = GetMethodName(expression);
        var handler = OperationHandlerRegistry.GetHandler<TResult>(operationName, expression);
        return handler.ExecuteAsync(expression, GetDbSets(expression), _interfaceType, cancellationToken);
    }


    private string GetMethodName(Expression expression)
    {
        if (expression is MethodCallExpression methodCall)
        {
            return methodCall.Method.Name;
        }

        return string.Empty;
    }

    private IEnumerable<IQueryable> GetDbSets(Expression expression)
    {
        // Check if expression contains OfType<T> calls and filter entity types accordingly
        var ofTypeTargets = ExtractOfTypeTargets(expression);

        if (ofTypeTargets.Count > 0)
        {
            // Only return DbSets for entity types that are assignable to the OfType target types
            var filteredTypes = _entityTypes
                .Where(entityType => ofTypeTargets.Any(targetType => targetType.IsAssignableFrom(entityType)))
                .ToList();

            return filteredTypes.Count > 0
                ? filteredTypes.Select(GetDbSet)
                : _entityTypes.Select(GetDbSet); // Fallback if filtering results in empty set
        }

        return _entityTypes.Select(GetDbSet);
    }

    private static List<Type> ExtractOfTypeTargets(Expression expression)
    {
        var visitor = new OfTypeExpressionVisitor();
        visitor.Visit(expression);
        return visitor.OfTypeTargets;
    }

    private class OfTypeExpressionVisitor : ExpressionVisitor
    {
        public List<Type> OfTypeTargets { get; } = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Check if this is an OfType<T> call
            if (node.Method.Name == "OfType" && node.Method.IsGenericMethod)
            {
                var targetType = node.Method.GetGenericArguments()[0];
                if (!OfTypeTargets.Contains(targetType))
                {
                    OfTypeTargets.Add(targetType);
                }
            }

            return base.VisitMethodCall(node);
        }
    }

    private IQueryable GetDbSet(Type entityType)
    {
        // Use DbContext.Set<TEntity>() method via reflection
        var setMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)
            ?.MakeGenericMethod(entityType);

        if (setMethod == null)
        {
            throw new InvalidOperationException($"Could not find Set<T>() method for entity type {entityType.Name}");
        }

        var dbSet = setMethod.Invoke(_context, null);
        return (IQueryable)dbSet!;
    }
}