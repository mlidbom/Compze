using Compze.Tessaging.Abstractions.Public;
using Compze.Tessaging.Abstractions.TessageTypes;
using Compze.Teventive.Infrastructure.EventDispatching;

namespace Compze.Teventive;

public interface IMutableTeventDispatcher<in TTevent> : ITeventDispatcher<TTevent>
   where TTevent : class, ITevent
{
   ///<summary>Creates a new <see cref="ITeventSubscriber{TTevent}"/> through which handlers subscribe to this dispatcher's tevents.<br/>
   /// Dispose the subscriber to remove every subscription made through it.</summary>
   ITeventSubscriber<TTevent> Register();

   ///<summary>Returns true if this dispatcher has any handlers that would handle the given tevent.</summary>
   bool Handles(TTevent tevent);

   ///<summary>Creates a dispatcher configured by <paramref name="config"/>, or by <see cref="TeventDispatcherConfig.Default"/> when none are supplied.</summary>
   static IMutableTeventDispatcher<TTevent> New(TeventDispatcherConfig? config = null) => new CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent>(config ?? TeventDispatcherConfig.Default);
}
