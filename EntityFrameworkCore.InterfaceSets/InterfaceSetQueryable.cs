using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.InterfaceSets;

public class InterfaceSetQueryable<TInterface,TResult> : IQueryable<TResult>, IAsyncEnumerable<TResult>
{
    public Type ElementType => typeof(TResult);
    public Expression Expression => _expression;
    public IQueryProvider Provider => _provider;
    
    private readonly Expression _expression;
    private readonly InterfaceSetQueryProvider<TInterface> _provider;

    internal InterfaceSetQueryable(DbContext context, Type interfaceType)
    {
        _provider = new InterfaceSetQueryProvider<TInterface>(context,interfaceType);
        _expression = Expression.Constant(this);
    }

    internal InterfaceSetQueryable(Expression expression,InterfaceSetQueryProvider<TInterface> provider)
    {
        _expression = expression;
        _provider = provider;
    }

    public IEnumerator<TResult> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<TResult>>(_expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return _provider.ExecuteAsync<IAsyncEnumerable<TResult>>(_expression, cancellationToken).GetAsyncEnumerator(cancellationToken);
    }
}