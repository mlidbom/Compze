using Compze.Tessaging.TessageTypes;
using Compze.Teventive._private.Infrastructure.EventDispatching;

namespace Compze.Teventive;

///<summary>Dispatches each tevent to the handlers subscribed to a type it is assignable to, synchronously, on the thread that dispatches it.<br/>
/// Handlers subscribe through the <see cref="ITeventSubscriber{TTevent}"/> that <see cref="Register"/> returns, and unsubscribe by disposing it.</summary>
///<remarks>Safe to use from several threads at once: to dispatch, to register handlers and to dispose subscribers.<br/>
/// The dispatcher holds no lock while the handlers run. A handler may itself dispatch, register handlers or dispose a subscriber,<br/>
/// and dispatches on several threads call their handlers at the same time: a handler that changes state other threads use must protect that state itself.<br/>
/// Each dispatch calls the handlers subscribed when it began. A handler subscribed, or a subscriber disposed, on another thread while a dispatch runs<br/>
/// may or may not take part in that dispatch, as with a .NET event raised on one thread while another thread subscribes or unsubscribes.</remarks>
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
