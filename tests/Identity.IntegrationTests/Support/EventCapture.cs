namespace Identity.IntegrationTests.Support;

/// <summary>Registered as a singleton in a subscriber's own DI container so a test can await what its handler received.</summary>
internal sealed class EventCapture<TEvent>
{
    public TaskCompletionSource<(TEvent Body, string? TypeHeader)> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
