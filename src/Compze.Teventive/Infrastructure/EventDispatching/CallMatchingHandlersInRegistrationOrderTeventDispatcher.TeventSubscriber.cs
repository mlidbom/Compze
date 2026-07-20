using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging;
using Compze.Tessaging.TessageTypes;
using TessageTypeInspector = Compze.Tessaging.Validation.Internal.TessageTypeInspector;

namespace Compze.Teventive.Infrastructure.EventDispatching;

partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   public sealed class TeventSubscriber(CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> owner) : ITeventSubscriber<TTevent>
   {
      readonly CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> _owner = owner;
      readonly List<RegisteredHandler> _handlerSubscriptions = [];
      readonly List<RegisteredHandler> _beforeHandlersSubscriptions = [];
      readonly List<RegisteredHandler> _afterHandlersSubscriptions = [];
      bool _isDisposed;

      ///<summary>Registers a handler for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
      TeventSubscriber For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent => ForGenericTevent(handler);

      ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
      /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
      /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
      /// </summary>
      TeventSubscriber ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent
      {
         AssertNotDisposed();
         AssertValidInnerTeventSubscription<THandledTevent>();
         AddSubscription(new RegisteredHandler<THandledTevent>(handler), _owner._handlers, _handlerSubscriptions);
         return this;
      }

      TeventSubscriber ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherTevent<TTevent> => ForWrappedGeneric(handler);

      TeventSubscriber ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherTevent<ITevent>
      {
         AssertNotDisposed();
         TessageTypeInspector.AssertValidForSubscription(typeof(TWrapperTevent));
         AddSubscription(new RegisteredWrappedHandler<TWrapperTevent>(handler), _owner._handlers, _handlerSubscriptions);
         return this;
      }

      TeventSubscriber BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) where THandledTevent : ITevent
      {
         AssertNotDisposed();
         AssertValidInnerTeventSubscription<THandledTevent>();
         AddSubscription(new RegisteredHandler<THandledTevent>(runBeforeHandlers), _owner._runBeforeHandlers, _beforeHandlersSubscriptions);
         return this;
      }

      TeventSubscriber AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) where THandledTevent : ITevent
      {
         AssertNotDisposed();
         AssertValidInnerTeventSubscription<THandledTevent>();
         AddSubscription(new RegisteredHandler<THandledTevent>(runAfterHandlers), _owner._runAfterHandlers, _afterHandlersSubscriptions);
         return this;
      }

      ///<summary>Removes every subscription made through this subscriber from the owning dispatcher. Disposing again does nothing.</summary>
      public void Dispose()
      {
         if(_isDisposed) return;
         _isDisposed = true;

         _handlerSubscriptions.ForEach(subscription => _owner._handlers.Remove(subscription));
         _beforeHandlersSubscriptions.ForEach(subscription => _owner._runBeforeHandlers.Remove(subscription));
         _afterHandlersSubscriptions.ForEach(subscription => _owner._runAfterHandlers.Remove(subscription));
         _handlerSubscriptions.Clear();
         _beforeHandlersSubscriptions.Clear();
         _afterHandlersSubscriptions.Clear();
         _owner._registrationVersion++;
      }

      void AddSubscription(RegisteredHandler subscription, List<RegisteredHandler> dispatcherSubscriptions, List<RegisteredHandler> mySubscriptions)
      {
         dispatcherSubscriptions.Add(subscription);
         mySubscriptions.Add(subscription);
         _owner._registrationVersion++;
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
