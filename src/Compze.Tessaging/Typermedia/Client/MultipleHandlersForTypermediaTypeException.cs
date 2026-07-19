using Compze.Tessaging.Endpoints;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>Thrown when a typermedia tessage is executed while more than one connected endpoint advertises handling its type.<br/>
/// A typermedia tessage executes on exactly one handler, and with several live handlers there is no way to know which is<br/>
/// intended, so the send fails loud naming the endpoints — never a crash of the topology machinery and never a silent pick.<br/>
/// For an endpoint navigating remote typermedia this is exclusively the patience-exhausted failure: the send first waited,<br/>
/// bounded, for the ambiguity to resolve — a rolling replacement resolves it the moment the retiring endpoint retracts. An<br/>
/// external client application's router throws it immediately instead: the client's connections change only by its own<br/>
/// explicit connects, so there is nothing to wait for.</summary>
public class MultipleHandlersForTypermediaTypeException : Exception
{
   MultipleHandlersForTypermediaTypeException(string message) : base(message) {}

   internal static MultipleHandlersForTypermediaTypeException BecausePatienceIsExhausted(Type tessageType, TimeSpan patience, IReadOnlyList<EndpointId> handlerEndpointIds) => new(
      $"More than one connected endpoint advertises handling the typermedia type {tessageType.GetFullNameCompilable()} ({string.Join(", ", handlerEndpointIds)}), and the ambiguity did not resolve within the endpoint's handler-availability patience (waited {patience.TotalSeconds:0.###}s). "
      + "A typermedia tessage executes on exactly one handler, and with several live handlers there is no way to know which is intended. This happens when a replacement endpoint runs alongside the endpoint it replaces: retire one of them.");

   internal static MultipleHandlersForTypermediaTypeException AmongTheClientsConnectedEndpoints(Type tessageType, IReadOnlyList<EndpointId> handlerEndpointIds) => new(
      $"More than one connected endpoint advertises handling the typermedia type {tessageType.GetFullNameCompilable()} ({string.Join(", ", handlerEndpointIds)}). A typermedia tessage executes on exactly one handler, and with several live handlers there is no way to know which is intended. This happens when a replacement endpoint runs alongside the endpoint it replaces: retire one of them.");
}
