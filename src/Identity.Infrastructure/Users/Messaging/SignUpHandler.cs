using BuildingBlocks.Messaging.RequestReply;
using Identity.Application.Common;
using Identity.Application.Users.SignUp;
using Identity.Contracts.Users;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Identity.Infrastructure.Users.Messaging;

/// <summary>
/// Thin by design (CONVENTIONS.md "Messaging" — no business logic in a handler): translate the wire
/// command into <see cref="SignUpRequest"/>, call the one input port, translate the
/// <c>Result</c> back into a wire reply — a success payload, or
/// <see cref="ReplyFault"/> on failure (the bridge, not the reply type, carries failure; see
/// <see cref="SignUpReply"/>'s doc comment). Everything that decides the outcome lives behind
/// <see cref="IInteractor{TRequest,TResponse}"/>.
/// </summary>
internal sealed class SignUpHandler : IHandleMessages<SignUp>
{
    private readonly IInteractor<SignUpRequest, SignUpResponse> _signUp;
    private readonly IBus _bus;

    public SignUpHandler(IInteractor<SignUpRequest, SignUpResponse> signUp, IBus bus)
    {
        _signUp = signUp;
        _bus = bus;
    }

    public async Task Handle(SignUp message)
    {
        // The message's own token, not CancellationToken.None (CONVENTIONS.md "Style"): an abandoned
        // message — the requester gave up, or the process is shutting down — should stop this
        // handler's work too, including a work-factor-12 BCrypt hash that would otherwise run to
        // completion for no one.
        var cancellationToken = MessageContext.Current.GetCancellationToken();

        var request = new SignUpRequest(message.Email, message.Password, message.DisplayName);
        var result = await _signUp.Handle(request, cancellationToken);

        object reply = result.Match<object>(
            onSuccess: response => new SignUpReply(response.UserId, response.AccessToken, response.AccessTokenExpiresAt),
            onFailure: ReplyFault.From);

        await _bus.Reply(reply);
    }
}
