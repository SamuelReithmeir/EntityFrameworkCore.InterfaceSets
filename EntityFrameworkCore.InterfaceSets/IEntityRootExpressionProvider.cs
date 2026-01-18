using System.Linq.Expressions;

namespace EntityFrameworkCore.InterfaceSets;

public interface IEntityRootExpressionProvider
{
    public Expression GetEntityRootExpression(Type entityType);
}