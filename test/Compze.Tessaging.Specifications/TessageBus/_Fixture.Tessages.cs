using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Specifications.TessageBus;

///<summary>An exactly-once tommand whose handler waits for the token it is handed to be cancelled.</summary>
public class TommandWhoseHandlerWaitsForItsCancellation : Remotable.ExactlyOnce.Tommand;

#pragma warning disable CA1040 // Marker interfaces used for type-routing — a tevent's interface hierarchy IS its routing
///<summary>A tevent whose handler waits for the token it is handed to be cancelled.</summary>
public interface ITeventWhoseHandlerWaitsForItsCancellation : ITevent;
#pragma warning restore CA1040

public class TeventWhoseHandlerWaitsForItsCancellation : ITeventWhoseHandlerWaitsForItsCancellation;

///<summary>An exactly-once tommand no endpoint handles: a send of it waits for a receiver to bind it to.</summary>
public class TommandNoEndpointHandles : Remotable.ExactlyOnce.Tommand;
