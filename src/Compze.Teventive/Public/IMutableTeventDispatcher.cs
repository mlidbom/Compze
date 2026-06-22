using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Teventive.Infrastructure.EventDispatching;

namespace Compze.Tessaging.Teventive.Public;

public interface IMutableTeventDispatcher<in TTevent> : ITeventDispatcher<TTevent>
   where TTevent : class, ITevent
{
   ///<summary>Registers handlers for the incoming tevents. All matching handlers will be called in the order they were registered.</summary>
   ITeventHandlerRegistrar<TTevent> Register();

   ///<summary>Returns true if this dispatcher has any handlers that would handle the given tevent.</summary>
   bool Handles(TTevent tevent);

   static IMutableTeventDispatcher<TTevent> New() => new CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent>();
}