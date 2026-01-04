using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Custom query provider that supports async operations for InterfaceSet queries.
/// This provider enables async LINQ methods like FirstAsync, ToListAsync, CountAsync, etc.
/// </summary>
/// <typeparam name="TInterface">The interface type being queried.</typeparam>
internal class InterfaceSetAsyncQueryProvider<TInterface> : IAsyncQueryProvider
    where TInterface : class
{
    private readonly DbContext _context;
    private readonly List<Type> _entityTypes;
    private readonly InterfaceSetEnumerable<TInterface> _enumerable;

    /// <summary>
    /// Initializes a new instance of the InterfaceSetAsyncQueryProvider class.
    /// </summary>
    /// <param name="context">The DbContext containing the entity types.</param>
    /// <param name="entityTypes">The list of entity types that implement the interface.</param>
    /// <param name="enumerable">The enumerable that handles iteration.</param>
    public InterfaceSetAsyncQueryProvider(
        DbContext context,
        List<Type> entityTypes,
        InterfaceSetEnumerable<TInterface> enumerable)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _entityTypes = entityTypes ?? throw new ArgumentNullException(nameof(entityTypes));
        _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
    }

    /// <summary>
    /// Creates a queryable from an expression.
    /// </summary>
    public IQueryable CreateQuery(Expression expression)
    {
        throw new NotSupportedException(
            "Non-generic CreateQuery is not supported. Use the generic version.");
    }

    /// <summary>
    /// Creates a typed queryable from an expression.
    /// </summary>
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        // For LINQ operations that return modified queries (Where, OrderBy, etc.),
        // we wrap the expression in a new queryable that still uses this provider
        return new InterfaceSetQueryable<TElement>(expression, this);
    }

    /// <summary>
    /// Executes a query synchronously.
    /// </summary>
    public object? Execute(Expression expression)
    {
        // For synchronous execution, we materialize the query and execute it in-memory
        var elementType = expression.Type.GetGenericArguments().FirstOrDefault() ?? typeof(TInterface);
        var method = typeof(InterfaceSetAsyncQueryProvider<TInterface>)
            .GetMethod(nameof(ExecuteQuerySync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.MakeGenericMethod(elementType);

        return method?.Invoke(this, new object[] { expression });
    }

    /// <summary>
    /// Executes a typed query synchronously.
    /// </summary>
    public TResult Execute<TResult>(Expression expression)
    {
        return (TResult)ExecuteQuerySync<TResult>(expression)!;
    }

    /// <summary>
    /// Executes a query asynchronously.
    /// </summary>
    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        // Get the actual result type from TResult
        // TResult will be Task<T> or similar async wrapper
        var resultType = typeof(TResult);

        if (!resultType.IsGenericType)
        {
            throw new InvalidOperationException($"Async execution requires a generic Task type, but got {resultType.Name}");
        }

        var taskResultType = resultType.GetGenericArguments()[0];

        // Use reflection to call the appropriate async execution method
        var method = typeof(InterfaceSetAsyncQueryProvider<TInterface>)
            .GetMethod(nameof(ExecuteQueryAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.MakeGenericMethod(taskResultType);

        if (method == null)
        {
            throw new InvalidOperationException("Could not find ExecuteQueryAsync method");
        }

        var task = method.Invoke(this, new object[] { expression, cancellationToken });
        return (TResult)task!;
    }

    /// <summary>
    /// Executes a query synchronously by materializing through the enumerable.
    /// </summary>
    private TResult? ExecuteQuerySync<TResult>(Expression expression)
    {
        // Build a queryable from our enumerable and compile the expression
        var queryable = _enumerable.AsQueryable();
        var rewritten = new ExpressionReplacer(queryable.Expression, expression).Visit(expression);

        var lambda = Expression.Lambda<Func<TResult>>(rewritten);
        var compiled = lambda.Compile();

        return compiled();
    }

    /// <summary>
    /// Executes a query asynchronously by coordinating across multiple DbSets.
    /// </summary>
    private async Task<TResult> ExecuteQueryAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        // Materialize the expression into a strongly-typed lambda we can execute
        // The expression contains the LINQ query to execute

        // Build the full query by applying the expression to our enumerable
        var queryable = _enumerable.AsQueryable();

        // For async operations like FirstAsync, ToListAsync, etc., we need to:
        // 1. Execute queries against each entity type's DbSet
        // 2. Combine results in memory
        // 3. Apply the final operation

        var methodName = GetMethodName(expression);

        return methodName switch
        {
            "First" or "FirstOrDefault" => await ExecuteFirstAsync<TResult>(expression, methodName == "FirstOrDefault", cancellationToken),
            "Single" or "SingleOrDefault" => await ExecuteSingleAsync<TResult>(expression, methodName == "SingleOrDefault", cancellationToken),
            "Count" or "LongCount" => ExecuteCount<TResult>(expression),
            "Any" => ExecuteAny<TResult>(expression),
            "ToList" or "ToArray" => await ExecuteToCollectionAsync<TResult>(expression, cancellationToken),
            _ => throw new NotSupportedException($"Async operation '{methodName}' is not supported by InterfaceSet")
        };
    }

    private async Task<TResult> ExecuteFirstAsync<TResult>(Expression expression, bool orDefault, CancellationToken cancellationToken)
    {
        // Extract the predicate if there is one
        var predicate = ExtractPredicate<TInterface>(expression);

        // Query each entity type and get the first match
        foreach (var entityType in _entityTypes)
        {
            var dbSet = EntityTypeDiscovery.GetDbSet(_context, entityType);
            var query = ApplyPredicate(dbSet, predicate);

            // Try to get the first item from this entity type
            var asyncQuery = query.Cast<TInterface>();

            if (orDefault)
            {
                var result = await asyncQuery.FirstOrDefaultAsync(cancellationToken);
                if (result != null)
                {
                    return (TResult)(object)result!;
                }
            }
            else
            {
                try
                {
                    var result = await asyncQuery.FirstAsync(cancellationToken);
                    return (TResult)(object)result!;
                }
                catch (InvalidOperationException)
                {
                    // No items in this set, try next one
                }
            }
        }

        if (orDefault)
        {
            return default!;
        }

        throw new InvalidOperationException("Sequence contains no elements");
    }

    private async Task<TResult> ExecuteSingleAsync<TResult>(Expression expression, bool orDefault, CancellationToken cancellationToken)
    {
        var predicate = ExtractPredicate<TInterface>(expression);
        var allResults = new List<TInterface>();

        // Collect all matching items from all entity types
        foreach (var entityType in _entityTypes)
        {
            var dbSet = EntityTypeDiscovery.GetDbSet(_context, entityType);
            var query = ApplyPredicate(dbSet, predicate);
            var results = await query.Cast<TInterface>().ToListAsync(cancellationToken);
            allResults.AddRange(results);
        }

        if (orDefault)
        {
            return (TResult)(object)allResults.SingleOrDefault()!;
        }

        return (TResult)(object)allResults.Single()!;
    }

    private TResult ExecuteCount<TResult>(Expression expression)
    {
        var predicate = ExtractPredicate<TInterface>(expression);
        long count = 0;

        foreach (var entityType in _entityTypes)
        {
            var dbSet = EntityTypeDiscovery.GetDbSet(_context, entityType);
            var query = ApplyPredicate(dbSet, predicate);

            if (typeof(TResult) == typeof(long))
            {
                count += query.Cast<TInterface>().LongCount();
            }
            else
            {
                count += query.Cast<TInterface>().Count();
            }
        }

        return (TResult)(object)Convert.ChangeType(count, typeof(TResult))!;
    }

    private TResult ExecuteAny<TResult>(Expression expression)
    {
        var predicate = ExtractPredicate<TInterface>(expression);

        foreach (var entityType in _entityTypes)
        {
            var dbSet = EntityTypeDiscovery.GetDbSet(_context, entityType);
            var query = ApplyPredicate(dbSet, predicate);

            if (query.Cast<TInterface>().Any())
            {
                return (TResult)(object)true!;
            }
        }

        return (TResult)(object)false!;
    }

    private async Task<TResult> ExecuteToCollectionAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        var items = new List<TInterface>();

        // Extract any Where/OrderBy/etc. from the expression
        var queryable = _enumerable.AsQueryable();
        var provider = queryable.Provider;
        var resultQuery = provider.CreateQuery<TInterface>(expression);

        // Enumerate the results
        await foreach (var item in _enumerable.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }

        // Apply the compiled expression to the materialized list
        var compiledQuery = resultQuery.Expression;
        var lambda = Expression.Lambda<Func<IEnumerable<TInterface>>>(
            Expression.Call(
                typeof(Enumerable),
                nameof(Enumerable.AsEnumerable),
                new[] { typeof(TInterface) },
                Expression.Constant(items)
            )
        );

        // Return as list or array
        if (typeof(TResult).IsArray)
        {
            return (TResult)(object)items.ToArray()!;
        }

        return (TResult)(object)items!;
    }

    private string GetMethodName(Expression expression)
    {
        if (expression is MethodCallExpression methodCall)
        {
            return methodCall.Method.Name;
        }

        return string.Empty;
    }

    private Expression<Func<TInterface, bool>>? ExtractPredicate<T>(Expression expression)
    {
        // Walk the expression tree to find any Where clause or predicate parameter
        if (expression is MethodCallExpression methodCall)
        {
            // Check if this is a Where call
            if (methodCall.Method.Name == "Where" && methodCall.Arguments.Count >= 2)
            {
                var predicateArg = methodCall.Arguments[1];
                if (predicateArg is UnaryExpression unary && unary.Operand is LambdaExpression lambda)
                {
                    return (Expression<Func<TInterface, bool>>)lambda;
                }
            }

            // Check if this is a First/Count/Any/Single with a predicate parameter
            if ((methodCall.Method.Name == "First" ||
                 methodCall.Method.Name == "FirstOrDefault" ||
                 methodCall.Method.Name == "Single" ||
                 methodCall.Method.Name == "SingleOrDefault" ||
                 methodCall.Method.Name == "Count" ||
                 methodCall.Method.Name == "LongCount" ||
                 methodCall.Method.Name == "Any") &&
                methodCall.Arguments.Count >= 2)
            {
                var predicateArg = methodCall.Arguments[1];
                if (predicateArg is UnaryExpression unary && unary.Operand is LambdaExpression lambda)
                {
                    return (Expression<Func<TInterface, bool>>)lambda;
                }
                else if (predicateArg is LambdaExpression directLambda)
                {
                    return (Expression<Func<TInterface, bool>>)directLambda;
                }
            }

            // Recursively check the source
            if (methodCall.Arguments.Count > 0)
            {
                return ExtractPredicate<T>(methodCall.Arguments[0]);
            }
        }

        return null;
    }

    private IQueryable ApplyPredicate(IQueryable query, Expression<Func<TInterface, bool>>? predicate)
    {
        if (predicate == null)
        {
            return query;
        }

        // Need to convert the predicate to work with the specific entity type
        var parameter = Expression.Parameter(query.ElementType, "x");
        var converted = Expression.Lambda(
            new ParameterReplacementVisitor(predicate.Parameters[0], parameter).Visit(predicate.Body),
            parameter
        );

        var whereMethod = typeof(Queryable)
            .GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
            .MakeGenericMethod(query.ElementType);

        return (IQueryable)whereMethod.Invoke(null, new object[] { query, converted })!;
    }

    /// <summary>
    /// Visitor to replace parameters in expressions.
    /// </summary>
    private class ParameterReplacementVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly ParameterExpression _newParameter;

        public ParameterReplacementVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
        {
            _oldParameter = oldParameter;
            _newParameter = newParameter;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == _oldParameter ? _newParameter : base.VisitParameter(node);
        }
    }

    /// <summary>
    /// Visitor to replace the constant queryable source.
    /// </summary>
    private class ExpressionReplacer : ExpressionVisitor
    {
        private readonly Expression _oldExpression;
        private readonly Expression _newExpression;

        public ExpressionReplacer(Expression oldExpression, Expression newExpression)
        {
            _oldExpression = oldExpression;
            _newExpression = newExpression;
        }

        public override Expression Visit(Expression? node)
        {
            if (node == _oldExpression)
            {
                return _newExpression;
            }
            return base.Visit(node)!;
        }
    }
}

/// <summary>
/// Custom queryable that uses our async query provider.
/// </summary>
internal class InterfaceSetQueryable<TElement> : IOrderedQueryable<TElement>, IAsyncEnumerable<TElement>
{
    private readonly Expression _expression;
    private readonly IQueryProvider _provider;

    public InterfaceSetQueryable(Expression expression, IQueryProvider provider)
    {
        _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public Type ElementType => typeof(TElement);

    public Expression Expression => _expression;

    public IQueryProvider Provider => _provider;

    public IEnumerator<TElement> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<TElement>>(_expression).GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAsyncEnumerator<TElement> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        // This should delegate to the provider's async enumeration if available
        throw new NotSupportedException("Direct async enumeration on modified queries is not yet supported. Use await foreach on the original InterfaceSet or materialize with ToListAsync() first.");
    }
}
