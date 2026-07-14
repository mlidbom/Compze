using System.Reflection;

namespace Compze.Internals.Transport.AspNet;

///<summary>One communication style's request handling, contributed to the endpoint's ASP.NET Core transport server<br/>
/// (<see cref="AspNetCoreEndpointTransportServer"/>): the assembly holding the style's controllers, added to the server's<br/>
/// application parts so its routes are served. Registered as a component set member (<c>Singleton.ForSet</c>) by the style's<br/>
/// ASP.NET Core transport registration; the server resolves the whole set and hosts the union.</summary>
///<remarks><see cref="InfrastructureQueryController"/> is never contributed — the server hosts it itself, because every endpoint<br/>
/// serves discovery no matter what it speaks.</remarks>
public sealed class AspNetCoreControllerContribution
{
   ///<summary>The assembly holding the communication style's controllers.</summary>
   public Assembly ControllerAssembly { get; }

   public AspNetCoreControllerContribution(Assembly controllerAssembly) => ControllerAssembly = controllerAssembly;
}
