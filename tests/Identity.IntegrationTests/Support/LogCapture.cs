using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Identity.IntegrationTests.Support;

/// <summary>
/// Captures log entries written by the Identity host, so a test can assert that something was
/// LOGGED rather than only that it did not throw.
/// </summary>
/// <remarks>
/// Ported from GiftLists' own <c>LogCapture</c> (added there for <c>GiftListEventPublisherTests</c>
/// after a Batch 12 review found "does not throw" alone stayed green with the <c>LogCritical</c>
/// call deleted outright) for GL-62's <c>UserEventPublisherFailureTests</c>, which hit the exact
/// same gap: Ryan's decision to swallow rests explicitly on the log being the mitigation, so the
/// log IS the contract here too, not merely how the contract reports itself.
/// </remarks>
public sealed class LogCapture : ILoggerProvider
{
    private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => _entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<(LogLevel, string)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
