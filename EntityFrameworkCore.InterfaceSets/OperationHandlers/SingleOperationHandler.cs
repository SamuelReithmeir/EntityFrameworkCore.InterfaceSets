using System.Linq.Expressions;
using System.Reflection;
using EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers;

public class SingleOperationHandler<TResult> : BaseOperationHandler<TResult>
{
    public override bool CanHandle(string operationName, Expression expression) => operationName == "Single";

    public override async Task<TResult> ExecuteAsync(Expression expression, IEnumerable<IQueryable> dbSets,
        Type interfaceType, CancellationToken cancellationToken = default)
    {
        List<TResult?> results = [];
        foreach (var dbSet in dbSets)
        {
            try
            {
                var result = await ExecuteInterfaceExpressionOnDbSetAsync(dbSet, expression, interfaceType,
                    cancellationToken);
                results.Add(result);
            }
            catch (InvalidOperationException)
            {
                results.Add(default);
            }
        }

        //perform single check over results of all dbSets
        if (results.OfType<TResult>().ToList() is not [var single])
        {
            throw new InvalidOperationException("Sequence contains more than one element");
        }

        return single;
    }

    public override TResult Execute(Expression expression, IEnumerable<IQueryable> dbSets, Type interfaceType)
    {
        List<TResult?> results = [];
        foreach (var dbSet in dbSets)
        {
            try
            {
                results.Add(ExecuteInterfaceExpressionOnDbSet(dbSet, expression, interfaceType));
            }
            catch (TargetInvocationException)
            {
                results.Add(default);
            }
        }

        //perform single check over results of all dbSets
        if (results.OfType<TResult>().ToList() is not [var single])
        {
            throw new InvalidOperationException("Sequence contains more than one element");
        }

        return single;
    }
}