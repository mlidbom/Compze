using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageTypes;
using Compze.Threading.ResourceAccess;
using TessageTypeInspector = Compze.Tessaging.Validation._internal.TessageTypeInspector;

namespace Compze.Teventive._private.Infrastructure.EventDispatching;

partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   ///<summary>The <see cref="ITeventSubscriber{TTevent}"/> <see cref="Register"/> returns. Its own state, like the dispatcher's <see cref="Subscriptions"/>,<br/>
   /// is used only under the dispatcher's lock, so a subscription made, or the subscriber disposed, on one thread is atomic for every other.</summary>
   sealed class TeventSubscriber(IThreadShared<Subscriptions> dispatcherSubscriptions) : ITeventSubscriber<TTevent>
   {
      readonly IThreadShared<Subscriptions> _dispatcherSubscriptions = dispatcherSubscriptions;
      ///<summary>The subscriptions made through this subscriber: what <see cref="Dispose"/> removes from the dispatcher.</summary>
      readonly List<RegisteredHandler> _subscriptions = [];
      bool _isDisposed;

      ///<summary>Registers a handler for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
      TeventSubscriber For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent => ForGenericTevent(handler);

      ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
      /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
      /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
      /// </summary>
      TeventSubscriber ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent
      {
         AssertValidInnerTeventSubscription<THandledTevent>();
         return Subscribe(new RegisteredHandler<THandledTevent>(handler), static (subscriptions, subscription) => subscriptions.AddHandler(subscription));
      }

      TeventSubscriber ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherTevent<TTevent> => ForWrappedGeneric(handler);

      TeventSubscriber ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherTevent<ITevent>
      {
         TessageTypeInspector.AssertValidForSubscription(typeof(TWrapperTevent));
         return Subscribe(new RegisteredWrappedHandler<TWrapperTevent>(handler), static (subscriptions, subscription) => subscriptions.AddHandler(subscription));
      }

      TeventSubscriber BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) where THandledTevent : ITevent
      {
         AssertValidInnerTeventSubscription<THandledTevent>();
         return Subscribe(new RegisteredHandler<THandledTevent>(runBeforeHandlers), static (subscriptions, subscription) => subscriptions.AddRunBeforeHandlers(subscription));
      }

      TeventSubscriber AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) where THandledTevent : ITevent
      {
         AssertValidInnerTeventSubscription<THandledTevent>();
         return Subscribe(new RegisteredHandler<THandledTevent>(runAfterHandlers), static (subscriptions, subscription) => subscriptions.AddRunAfterHandlers(subscription));
      }

      ///<summary>Removes every subscription made through this subscriber from the owning dispatcher. Disposing again does nothing.</summary>
      public void Dispose() => _dispatcherSubscriptions.Locked(dispatcherSubscriptions =>
      {
         if(_isDisposed) return;
         _isDisposed = true;

         dispatcherSubscriptions.Remove(_subscriptions);
         _subscriptions.Clear();
      });

      TeventSubscriber Subscribe(RegisteredHandler subscription, Action<Subscriptions, RegisteredHandler> addToTheDispatchersSubscriptions)
      {
         _dispatcherSubscriptions.Locked(dispatcherSubscriptions =>
         {
            AssertNotDisposed();
            addToTheDispatchersSubscriptions(dispatcherSubscriptions, subscription);
            _subscriptions.Add(subscription);
         });
         return this;
      }

      static void AssertValidInnerTeventSubscription<THandledTevent>() where THandledTevent : ITevent
      {
         TessageTypeInspector.AssertValidForSubscription(typeof(THandledTevent));
         if(typeof(THandledTevent).Is<IPublisherTevent<ITevent>>()) throw new Exception($"Handlers of type {typeof(IPublisherTevent<>).Name} must be registered through the {nameof(ForWrapped)} method.");
      }

      void AssertNotDisposed() => State.Assert(!_isDisposed, () => "This subscriber has been disposed: its subscriptions were removed from the dispatcher and no new ones can be registered through it.");

      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) => ForGenericTevent(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) => BeforeHandlers(runBeforeHandlers);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) => AfterHandlers(runAfterHandlers);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.For<THandledTevent>(Action<THandledTevent> handler) => For(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrapped(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrappedGeneric(handler);
   }
}
