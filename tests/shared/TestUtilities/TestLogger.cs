using Microsoft.Extensions.Logging;
using Moq;

namespace NuGone.Tests.Shared.TestUtilities;

/// <summary>
/// Test utility for LoggerMessage-based logging with proper configuration.
/// Provides helpers for testing LoggerMessage source generator implementations.
/// </summary>
/// <typeparam name="T">The type being logged</typeparam>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logEntries = new();
    private readonly LogLevel _minimumLevel;

    public TestLogger(LogLevel minimumLevel = LogLevel.Information)
    {
        _minimumLevel = minimumLevel;
    }

    /// <summary>
    /// Gets all logged entries.
    /// </summary>
    public IReadOnlyList<LogEntry> LogEntries => _logEntries.AsReadOnly();

    /// <summary>
    /// Gets entries logged at a specific level.
    /// </summary>
    public IEnumerable<LogEntry> GetEntriesAtLevel(LogLevel level) =>
        _logEntries.Where(e => e.Level == level);

    /// <summary>
    /// Gets entries containing specific text in their message.
    /// </summary>
    public IEnumerable<LogEntry> GetEntriesContaining(string text) =>
        _logEntries.Where(e => e.Message.Contains(text, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Asserts that a message was logged at the specified level.
    /// </summary>
    public void AssertLogged(LogLevel level, string expectedMessage)
    {
        var entry = _logEntries.FirstOrDefault(e =>
            e.Level == level
            && e.Message.Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)
        );

        if (entry == null)
        {
            var actualMessages = _logEntries
                .Where(e => e.Level == level)
                .Select(e => $"  - {e.Message}");

            throw new AssertionException(
                $"Expected log message at {level} level containing '{expectedMessage}' but found:\n{string.Join("\n", actualMessages)}"
            );
        }
    }

    /// <summary>
    /// Asserts that no messages were logged at the specified level.
    /// </summary>
    public void AssertNotLogged(LogLevel level, string unexpectedMessage)
    {
        var entry = _logEntries.FirstOrDefault(e =>
            e.Level == level
            && e.Message.Contains(unexpectedMessage, StringComparison.OrdinalIgnoreCase)
        );

        if (entry != null)
        {
            throw new AssertionException(
                $"Unexpected log message at {level} level containing '{unexpectedMessage}': {entry.Message}"
            );
        }
    }

    /// <summary>
    /// Asserts that exactly count messages were logged at the specified level.
    /// </summary>
    public void AssertLogCount(LogLevel level, int expectedCount)
    {
        var actualCount = _logEntries.Count(e => e.Level == level);
        if (actualCount != expectedCount)
        {
            throw new AssertionException(
                $"Expected {expectedCount} log entries at {level} level, but found {actualCount}"
            );
        }
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public void Clear() => _logEntries.Clear();

    // ILogger implementation
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => new NoOpDisposable();

    public bool IsEnabled(LogLevel level) => level >= _minimumLevel;

    public void Log<TState>(
        LogLevel level,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (!IsEnabled(level))
            return;

        var message = formatter(state, exception);
        _logEntries.Add(new LogEntry(level, eventId, message, exception, DateTime.UtcNow));
    }

    /// <summary>
    /// Creates a configured Mock<ILogger<T>> for use with LoggerMessage.
    /// </summary>
    public static Mock<ILogger<T>> CreateMockLogger(LogLevel minimumLevel = LogLevel.Information)
    {
        var mockLogger = new Mock<ILogger<T>>();
        mockLogger
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(level => level >= minimumLevel);

        // Capture log calls for verification
        mockLogger
            .As<ILogger>()
            .Setup(x =>
                x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                )
            )
            .Callback<LogLevel, EventId, object, Exception, Func<object, Exception?, string>>(
                (level, eventId, state, exception, formatter) =>
                {
                    if (level >= minimumLevel)
                    {
                        // This helps with debugging in tests
                        System.Diagnostics.Debug.WriteLine(
                            $"[{level}] {formatter(state, exception)}"
                        );
                    }
                }
            );

        return mockLogger;
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Represents a single log entry.
/// </summary>
public record LogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    DateTime Timestamp
);

/// <summary>
/// Custom assertion exception for test failures.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message)
        : base(message) { }
}

/// <summary>
/// Extension methods for Mock<ILogger<T>> to simplify LoggerMessage testing.
/// </summary>
public static class LoggerMockExtensions
{
    /// <summary>
    /// Enables all log levels for the mock logger (required for LoggerMessage).
    /// </summary>
    public static Mock<ILogger<T>> EnableAllLevels<T>(this Mock<ILogger<T>> mockLogger)
    {
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        return mockLogger;
    }

    /// <summary>
    /// Enables specific log levels for the mock logger.
    /// </summary>
    public static Mock<ILogger<T>> EnableLevels<T>(
        this Mock<ILogger<T>> mockLogger,
        params LogLevel[] levels
    )
    {
        mockLogger
            .Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
            .Returns<LogLevel>(level => levels.Contains(level));
        return mockLogger;
    }

    /// <summary>
    /// Verifies that a log call was made with the specified level and message content.
    /// </summary>
    public static Mock<ILogger<T>> VerifyLog<T>(
        this Mock<ILogger<T>> mockLogger,
        LogLevel level,
        string expectedMessage
    )
    {
        mockLogger.Verify(
            x =>
                x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>(
                        (state, _) =>
                            state
                                .ToString()!
                                .Contains(expectedMessage, StringComparison.OrdinalIgnoreCase)
                    ),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            $"Expected log at {level} level containing '{expectedMessage}' was not found"
        );

        return mockLogger;
    }

    /// <summary>
    /// Verifies that no log calls were made at the specified level.
    /// </summary>
    public static Mock<ILogger<T>> VerifyNoLog<T>(this Mock<ILogger<T>> mockLogger, LogLevel level)
    {
        mockLogger.Verify(
            x =>
                x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
            Times.Never,
            $"Unexpected log entries found at {level} level"
        );

        return mockLogger;
    }
}
