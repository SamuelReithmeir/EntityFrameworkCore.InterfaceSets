using Microsoft.Extensions.Logging;

namespace EntityFrameworkCore.InterfaceSets.Tests.Infrastructure;

public class SqlCommandLogger : ILogger
{
    private readonly List<string> _executedCommands = new();
    private readonly object _lock = new();

    public IReadOnlyList<string> ExecutedCommands
    {
        get
        {
            lock (_lock)
            {
                return _executedCommands.ToList();
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _executedCommands.Clear();
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        
        if (eventId.Name == "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted" ||
            eventId.Name == "CommandExecuted" ||
            message.Contains("Executed DbCommand"))
        {
            lock (_lock)
            {
                _executedCommands.Add(message);
            }
        }
    }
}

public class SqlCommandLoggerProvider : ILoggerProvider
{
    private readonly SqlCommandLogger _logger = new();

    public SqlCommandLogger Logger => _logger;

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
    }
}

