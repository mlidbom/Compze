using Compze.Abstractions.Hosting.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

///<summary>Thrown when a tommand is sent while more than one remembered peer advertises handling its type and none of them is<br/>
/// live — a handler replacement whose retired peer was never decommissioned. A tommand binds to one specific receiver at send<br/>
/// time so the pair's exactly-once in-order guarantee holds; with several remembered handlers and no live one there is no way<br/>
/// to know which is current, and binding to the wrong one would strand the tommand (see<br/>
/// <c>dev_docs/TODO/durable-peer-topology.md</c> — decommissioning the retired peer resolves this).</summary>
class MultipleHandlersForTessageTypeException(Type tommandType, IReadOnlyList<EndpointId> rememberedHandlerIds) : Exception(
   $"More than one remembered peer advertises handling the tommand type {tommandType.GetFullNameCompilable()} ({string.Join(", ", rememberedHandlerIds)}) and none of them is live. A tommand binds to one specific receiver at send time, and with several remembered handlers there is no way to know which is current. This happens after a handler replacement whose retired endpoint was never decommissioned: decommission it, or bring the current handler up.");
