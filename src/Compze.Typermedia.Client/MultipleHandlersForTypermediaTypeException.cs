using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Typermedia.Client;

///<summary>Thrown when a typermedia tessage is executed while more than one connected endpoint advertises handling its type.<br/>
/// A typermedia tessage executes on exactly one handler, and with several live handlers there is no way to know which is<br/>
/// intended, so the send fails loud naming the endpoints — never a crash of the topology machinery and never a silent pick.</summary>
public class MultipleHandlersForTypermediaTypeException(Type tessageType, IReadOnlyList<EndpointId> handlerEndpointIds) : Exception(
   $"More than one connected endpoint advertises handling the typermedia type {tessageType.GetFullNameCompilable()} ({string.Join(", ", handlerEndpointIds)}). A typermedia tessage executes on exactly one handler, and with several live handlers there is no way to know which is intended. This happens when a replacement endpoint runs alongside the endpoint it replaces: retire one of them.");
