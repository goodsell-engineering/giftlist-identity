using BuildingBlocks.Messaging.RequestReply;
using Identity.Application.Common;
using Identity.Application.Users.Login;
using Identity.Contracts.Users;
using Rebus.Bus;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Identity.Infrastructure.Users.Messaging;

/// <summary>Thin by design — see <see cref="SignUpHandler"/> for the rationale.</summary>
internal sealed class LoginHandler : IHandleMessages<Login>
{
    private readonly IInteractor<LoginRequest, LoginResponse> _login;
    private readonly IBus _bus;

    public LoginHandler(IInteractor<LoginRequest, LoginResponse> login, IBus bus)
    {
        _login = login;
        _bus = bus;
    }

    public async Task Handle(Login message)
    {
        var cancellationToken = MessageContext.Current.GetCancellationToken();

        var request = new LoginRequest(message.Email, message.Password);
        var result = await _login.Handle(request, cancellationToken);

        object reply = result.Match<object>(
            onSuccess: response => new LoginReply(response.UserId, response.AccessToken, response.AccessTokenExpiresAt),
            onFailure: ReplyFault.From);

        await _bus.Reply(reply);
    }
}
