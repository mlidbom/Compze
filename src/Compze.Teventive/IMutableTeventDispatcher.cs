using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive.Infrastructure.EventDispatching;

namespace Compze.Teventive;

public interface IMutableTeventDispatcher<in TTevent> : ITeventDispatcher<TTevent>
   where TTevent : class, ITevent
{
   ///<summary>Registers handlers for the incoming tevents. All matching handlers will be called in the order they were registered.</summary>
   ITeventHandlerRegistrar<TTevent> Register();

   ///<summary>Returns true if this dispatcher has any handlers that would handle the given tevent.</summary>
   bool Handles(TTevent tevent);

   ///<summary>Creates a dispatcher configured by <paramref name="config"/>, or by <see cref="TeventDispatcherConfig.Default"/> when none are supplied.</summary>
   static IMutableTeventDispatcher<TTevent> New(TeventDispatcherConfig? config = null) => new CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent>(config ?? TeventDispatcherConfig.Default);
}
