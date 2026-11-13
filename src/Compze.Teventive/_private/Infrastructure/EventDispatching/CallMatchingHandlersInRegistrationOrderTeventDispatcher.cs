// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Threading.ResourceAccess;

namespace Compze.Teventive._private.Infrastructure.EventDispatching;

/// <summary>
/// Calls all matching handlers in the order they were registered when a tevent is Dispatched.<br/>
/// Handlers are registered through the <see cref="ITeventSubscriber{TTevent}"/> that <see cref="Register"/> returns; disposing that subscriber removes every subscription made through it.
/// </summary>
/// <remarks>Safe to use from several threads at once. Every subscription, and the handlers each tevent type routes to, live in one <see cref="Subscriptions"/>,<br/>
/// which nothing reaches but through the lock of <see cref="_subscriptions"/>. The handlers run outside that lock: a handler may dispatch, register or dispose<br/>
/// a subscriber, on its own thread or any other, without deadlocking, and dispatches on several threads run their handlers at the same time.</remarks>
partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> : IMutableTeventDispatcher<TTevent>
   where TTevent : class, ITevent
{
   readonly IThreadShared<Subscriptions> _subscriptions;

   internal CallMatchingHandlersInRegistrationOrderTeventDispatcher(TeventDispatcherConfig config) => _subscriptions = IThreadShared.New(new Subscriptions(config));

   public ITeventSubscriber<TTevent> Register() => new TeventSubscriber(_subscriptions);

   public void Dispatch(TTevent evt) => Dispatch(PublisherTevent.WrapTevent(evt));

   public void Dispatch(IPublisherTevent<TTevent> wrapped)
   {
      var handlers = _subscriptions.Locked(it => it.HandlersToCallFor(wrapped.GetType()));
      for(var i = 0; i < handlers.Length; i++)
      {
         handlers[i](wrapped);
      }
   }

   public bool Handles(TTevent tevent) => _subscriptions.Locked(it => it.HasHandlersFor(PublisherTevent.WrapperTypeFor(tevent.GetType())));
}
