using Identity.Application.Common;

namespace Identity.Application.Users.SignUp;

/// <summary>
/// The named input port for "sign up" (CONVENTIONS.md "Naming"). Declared for the naming convention and
/// for readability on <see cref="SignUpInteractor"/>'s own base list — see
/// <see cref="IInteractor{TRequest,TResponse}"/>'s doc comment for why nothing resolves this
/// specific type from the container.
/// </summary>
public interface ISignUp : IInteractor<SignUpRequest, SignUpResponse>;
