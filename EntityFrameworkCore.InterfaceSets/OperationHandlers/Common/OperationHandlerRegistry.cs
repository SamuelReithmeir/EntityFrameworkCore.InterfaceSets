using System.Linq.Expressions;

namespace EntityFrameworkCore.InterfaceSets.OperationHandlers.Common;

public static class OperationHandlerRegistry
{
    private static readonly List<HandlerFactory> Factories = [];

    static OperationHandlerRegistry()
    {
        // Auto-register all handlers from assembly
        var handlerTypes = typeof(OperationHandlerRegistry).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IOperationHandler<>)));

        foreach (var handlerType in handlerTypes)
        {
            RegisterHandlerType(handlerType);
        }
    }

    private static void RegisterHandlerType(Type handlerType)
    {
        // If it's an open generic (EnumerableHandler<>, FirstOrDefaultHandler<>)
        if (handlerType.IsGenericTypeDefinition)
        {
            var handlerInterface = handlerType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType &&
                                     i.GetGenericTypeDefinition() == typeof(IOperationHandler<>));

            if (handlerInterface == null) return;

            var handlerResultType = handlerInterface.GetGenericArguments()[0];

            // Case 1: Handler result type is a generic pattern (e.g., IEnumerable<T>)
            if (handlerResultType is { IsGenericType: true, ContainsGenericParameters: true })
            {
                Factories.Add(new HandlerFactory
                {
                    IsPattern = true,
                    CanCreate = resultType =>
                    {
                        var handlerResultDefinition = handlerResultType.GetGenericTypeDefinition();
                        return resultType.IsGenericType &&
                               resultType.GetGenericTypeDefinition() == handlerResultDefinition;
                    },
                    Create = resultType =>
                    {
                        var typeArgs = resultType.GetGenericArguments();
                        var concreteType = handlerType.MakeGenericType(typeArgs);
                        return Activator.CreateInstance(concreteType)!;
                    }
                });
            }
            // Case 2: Handler result type is just T (e.g., FirstOrDefaultHandler<T>)
            else if (handlerResultType.IsGenericParameter)
            {
                Factories.Add(new HandlerFactory
                {
                    IsPattern = true,
                    CanCreate = _ => true, // Can handle any type
                    Create = resultType =>
                    {
                        var concreteType = handlerType.MakeGenericType(resultType);
                        return Activator.CreateInstance(concreteType)!;
                    }
                });
            }
        }
        else
        {
            // Concrete handler (CountHandler, CountLongHandler)
            var handlerInterface = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IOperationHandler<>));
            var resultType = handlerInterface.GetGenericArguments()[0];

            Factories.Add(new HandlerFactory
            {
                IsPattern = false,
                ResultType = resultType,
                Create = _ => Activator.CreateInstance(handlerType)!
            });
        }
    }

    public static IOperationHandler<TResult> GetHandler<TResult>(string operationName, Expression expression)
    {
        var requestedType = typeof(TResult);

        // Normalize nullable value types (int? -> int) and nullable reference types
        var underlyingType = Nullable.GetUnderlyingType(requestedType) ?? requestedType;

        // Try exact type match first (against underlying type for nullable)
        foreach (var factory in Factories.Where(f => !f.IsPattern && f.ResultType == underlyingType))
        {
            var handler = (IOperationHandler<TResult>)factory.Create(underlyingType);
            if (handler.CanHandle(operationName, expression))
                return handler;
        }

        // Try pattern match for generic types (against actual requested type)
        foreach (var factory in Factories.Where(f => f.IsPattern))
        {
            if (factory.CanCreate!(requestedType))
            {
                var handler = (IOperationHandler<TResult>)factory.Create(requestedType);
                if (handler.CanHandle(operationName, expression))
                    return handler;
            }
        }

        throw new NotSupportedException(
            $"No handler found for result type {requestedType.Name} with operation '{operationName}'");
    }

    private class HandlerFactory
    {
        public bool IsPattern { get; init; }
        public Type? ResultType { get; init; }
        public Func<Type, bool>? CanCreate { get; init; }
        public required Func<Type, object> Create { get; init; }
    }
}
