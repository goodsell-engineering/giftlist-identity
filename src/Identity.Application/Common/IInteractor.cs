using BuildingBlocks.Results;

namespace Identity.Application.Common;

/// <summary>
/// The shape every use case implements: one request in, one <see cref="Result{T}"/> out
/// (CONVENTIONS.md "Use cases"). <c>ISignUp</c>/<c>ILogin</c> and future ports each declare an empty,
/// specifically-named interface deriving from a closed instantiation of this one, purely so the
/// naming convention (`I` + imperative) and the "port implies an interactor and a request" text
/// scan (<c>NamingConventionTests.InputPorts_ShouldHaveAMatchingInteractorAndRequest</c>) both
/// hold. Nothing resolves a use case by its specifically-named port from the container, though:
/// a class cannot implement a specifically-named *derived* interface generically — only its
/// generic base — so the composition root, and the <c>Validating&lt;,&gt;</c>/<c>Logging&lt;,&gt;</c>
/// decorators (CONVENTIONS.md "Use cases"), all key on this interface instead. That is what makes one
/// decorator pair reusable across every use case rather than one hand-written decorator per port.
/// </summary>
public interface IInteractor<in TRequest, TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}
