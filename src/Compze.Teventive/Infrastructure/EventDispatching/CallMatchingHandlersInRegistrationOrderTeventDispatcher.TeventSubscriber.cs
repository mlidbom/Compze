using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Contracts;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Teventive.Infrastructure.EventDispatching;

partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   public sealed class TeventSubscriber(CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> owner) : ITeventSubscriber<TTevent>
   {
      readonly CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> _owner = owner;
      readonly List<RegisteredHandler> _handlerSubscriptions = [];
      readonly List<Action<object>> _beforeHandlersSubscriptions = [];
      readonly List<Action<object>> _afterHandlersSubscriptions = [];
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
         TessageTypeInspector.AssertValidForSubscription(typeof(THandledTevent));
         if(typeof(THandledTevent).Is<IPublisherIdentifyingTevent<ITevent>>()) throw new Exception($"Handlers of type {typeof(IPublisherIdentifyingTevent<>).Name} must be registered through the {nameof(ForWrapped)} method.");
         AddHandlerSubscription(new RegisteredHandler<THandledTevent>(handler));
         return this;
      }

      TeventSubscriber ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherIdentifyingTevent<TTevent> => ForWrappedGeneric(handler);

      TeventSubscriber ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherIdentifyingTevent<ITevent>
      {
         AssertNotDisposed();
         TessageTypeInspector.AssertValidForSubscription(typeof(TWrapperTevent));
         AddHandlerSubscription(new RegisteredWrappedHandler<TWrapperTevent>(handler));
         return this;
      }

      TeventSubscriber BeforeHandlers(Action<TTevent> runBeforeHandlers)
      {
         AssertNotDisposed();
         //Urgent: fix this. Use the registered handler classes above
         Action<object> subscription = tevent => runBeforeHandlers(((IPublisherIdentifyingTevent<TTevent>)tevent).Tevent);
         _owner._runBeforeHandlers.Add(subscription);
         _beforeHandlersSubscriptions.Add(subscription);
         _owner._registrationVersion++;
         return this;
      }

      TeventSubscriber AfterHandlers(Action<TTevent> runAfterHandlers)
      {
         AssertNotDisposed();
         //Urgent: fix this
         Action<object> subscription = tevent => runAfterHandlers(((IPublisherIdentifyingTevent<TTevent>)tevent).Tevent);
         _owner._runAfterHandlers.Add(subscription);
         _afterHandlersSubscriptions.Add(subscription);
         _owner._registrationVersion++;
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

      void AddHandlerSubscription(RegisteredHandler subscription)
      {
         _owner._handlers.Add(subscription);
         _handlerSubscriptions.Add(subscription);
         _owner._registrationVersion++;
      }

      void AssertNotDisposed() => State.Assert(!_isDisposed, () => "This subscriber has been disposed: its subscriptions were removed from the dispatcher and no new ones can be registered through it.");

      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) => ForGenericTevent(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) => BeforeHandlers(e => runBeforeHandlers((THandledTevent)e));
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) => AfterHandlers(e => runAfterHandlers((THandledTevent)e));
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.For<THandledTevent>(Action<THandledTevent> handler) => For(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrapped(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrappedGeneric(handler);
   }
}
