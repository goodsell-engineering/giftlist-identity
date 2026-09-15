using Identity.Contracts.Users.Events;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Identity.IntegrationTests.Support;

/// <summary>Captures a real, deserialized <see cref="UserRegisteredV1"/> plus the wire's own <c>rbs2-msg-type</c> header.</summary>
internal sealed class EventCapturingHandler(EventCapture<UserRegisteredV1> capture) : IHandleMessages<UserRegisteredV1>
{
    public Task Handle(UserRegisteredV1 message)
    {
        MessageContext.Current.Headers.TryGetValue(Headers.Type, out var typeHeader);
        capture.Completion.TrySetResult((message, typeHeader));
        return Task.CompletedTask;
    }
}
