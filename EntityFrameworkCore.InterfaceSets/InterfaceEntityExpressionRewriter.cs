using System.Linq.Expressions;
using System.Reflection;

namespace EntityFrameworkCore.InterfaceSets;

/// <summary>
/// Expression visitor that rewrites expressions for interface sets to be used on a concrete entity dbSet
/// </summary>
public sealed class InterfaceEntityExpressionRewriter : ExpressionVisitor
{
    private readonly Type _interfaceType;
    private readonly Type _entityType;

    // Tracks rewritten parameters to preserve reference equality
    private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterMap = new();

    public InterfaceEntityExpressionRewriter(Type interfaceType, Type entityType)
    {
        if (!interfaceType.IsAssignableFrom(entityType))
        {
            throw new ArgumentException(
                $"{entityType.FullName} is not assignable to {interfaceType.FullName}");
        }

        _interfaceType = interfaceType;
        _entityType = entityType;
    }

    private Type RewriteType(Type type)
    {
        if (type == _interfaceType)
            return _entityType;

        if (!type.IsGenericType)
        {
            return type;
        }
        var args = type.GetGenericArguments();
        var rewrittenArgs = args.Select(RewriteType).ToArray();

        if (!args.SequenceEqual(rewrittenArgs))
        {
            return type.GetGenericTypeDefinition().MakeGenericType(rewrittenArgs);
        }

        return type;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (_parameterMap.TryGetValue(node, out var mapped))
            return mapped;

        if (node.Type != _interfaceType)
            return node;

        var rewritten = Expression.Parameter(_entityType, node.Name);
        _parameterMap[node] = rewritten;
        return rewritten;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var parameters = node.Parameters
            .Select(p => (ParameterExpression)Visit(p))
            .ToArray();

        var body = Visit(node.Body);

        var delegateType = RewriteType(node.Type);

        return Expression.Lambda(delegateType, body, parameters);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var instance = Visit(node.Expression);

        var declaringType = RewriteType(node.Member.DeclaringType);

        if (declaringType == node.Member.DeclaringType)
            return node.Update(instance);

        var member = declaringType.GetMember(node.Member.Name,
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Single();

        return Expression.MakeMemberAccess(instance, member);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var instance = Visit(node.Object);
        var arguments = node.Arguments.Select(Visit).ToArray();

        var method = node.Method;

        if (method.IsGenericMethod)
        {
            var genericArgs = method.GetGenericArguments();
            var rewrittenArgs = genericArgs.Select(RewriteType).ToArray();

            if (!genericArgs.SequenceEqual(rewrittenArgs))
            {
                method = method.GetGenericMethodDefinition()
                    .MakeGenericMethod(rewrittenArgs);
            }
        }

        return Expression.Call(instance, method, arguments);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        return node.Update(
            Visit(node.Left),
            node.Conversion != null ? (LambdaExpression)Visit(node.Conversion) : null,
            Visit(node.Right));
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);
        var type = RewriteType(node.Type);

        return node.Type == type
            ? node.Update(operand)
            : Expression.MakeUnary(node.NodeType, operand, type, node.Method);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        var args = node.Arguments.Select(Visit).ToArray();

        var ctor = node.Constructor;
        if (ctor.DeclaringType == _interfaceType)
        {
            ctor = _entityType
                .GetConstructors()
                .Single(c => c.GetParameters().Length == ctor.GetParameters().Length);
        }

        return Expression.New(ctor, args, node.Members);
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        var rewrittenType = RewriteType(node.TypeOperand);

        return rewrittenType == node.TypeOperand
            ? base.VisitTypeBinary(node)
            : Expression.TypeIs(Visit(node.Expression), rewrittenType);
    }
    
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if(node.Type.IsGenericType && node.Type.GetGenericTypeDefinition() == typeof(InterfaceSet<>))
        {
            var entityRootExpressionProvider = (IEntityRootExpressionProvider)node.Value!;
            return entityRootExpressionProvider.GetEntityRootExpression(_entityType);
        }
        
        
        var rewrittenType = RewriteType(node.Type);

        if (rewrittenType == node.Type)
            return node;

        return Expression.Constant(node.Value, rewrittenType);
    }
}