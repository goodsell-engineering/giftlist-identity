using Identity.Application.Common;

namespace Identity.Application.Users.Login;

/// <summary>The named input port for "log in" — see <c>ISignUp</c> for why nothing resolves this specific type from the container.</summary>
public interface ILogin : IInteractor<LoginRequest, LoginResponse>;
