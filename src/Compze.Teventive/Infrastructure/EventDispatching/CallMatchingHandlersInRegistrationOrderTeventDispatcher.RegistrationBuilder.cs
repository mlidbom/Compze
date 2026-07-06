// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Internals.SystemCE.ReflectionCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Teventive.Infrastructure.EventDispatching;

/// <summary>
/// Calls all matching handlers in the order they were registered when an tevent is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   public sealed class TeventSubscriber(CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> owner) : ITeventSubscriber<TTevent>
   {
      readonly CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> _owner = owner;

      ///<summary>Registers a for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
      TeventSubscriber For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent => ForGenericTevent(handler);

      ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
      /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
      /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
      /// </summary>
      TeventSubscriber ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent
      {
         TessageTypeInspector.AssertValidForSubscription(typeof(THandledTevent));
         if(typeof(THandledTevent).Is<IPublisherIdentifyingTevent<ITevent>>()) throw new Exception($"Handlers of type {typeof(IPublisherIdentifyingTevent<>).Name} must be registered through the {nameof(ForWrapped)} method.");
         _owner._handlers.Add(new RegisteredHandler<THandledTevent>(handler));
         _owner._totalHandlers++;
         return this;
      }

      TeventSubscriber ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherIdentifyingTevent<TTevent> => ForWrappedGeneric(handler);

      TeventSubscriber ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherIdentifyingTevent<ITevent>
      {
         TessageTypeInspector.AssertValidForSubscription(typeof(TWrapperTevent));
         _owner._handlers.Add(new RegisteredWrappedHandler<TWrapperTevent>(handler));
         _owner._totalHandlers++;
         return this;
      }

      TeventSubscriber BeforeHandlers(Action<TTevent> runBeforeHandlers)
      {
         //Urgent: fix this. Use the registered handler classes above
         _owner._runBeforeHandlers.Add(e => runBeforeHandlers(((IPublisherIdentifyingTevent<TTevent>)e).Tevent));
         _owner._totalHandlers++;
         return this;
      }

      TeventSubscriber AfterHandlers(Action<TTevent> runAfterHandlers)
      {
         //Urgent: fix this
         _owner._runAfterHandlers.Add(e => runAfterHandlers(((IPublisherIdentifyingTevent<TTevent>)e).Tevent));
         _owner._totalHandlers++;
         return this;
      }

      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) => ForGenericTevent(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) => BeforeHandlers(e => runBeforeHandlers((THandledTevent)e));
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) => AfterHandlers(e => runAfterHandlers((THandledTevent)e));
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.For<THandledTevent>(Action<THandledTevent> handler) => For(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrapped(handler);
      ITeventSubscriber<TTevent> ITeventSubscriber<TTevent>.ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrappedGeneric(handler);
   }
}
