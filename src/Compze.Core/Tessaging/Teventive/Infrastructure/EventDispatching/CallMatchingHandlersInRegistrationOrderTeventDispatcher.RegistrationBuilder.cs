// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Internals.SystemCE.ReflectionCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Core.Tessaging.Teventive.Infrastructure.EventDispatching;

/// <summary>
/// Calls all matching handlers in the order they were registered when an tevent is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
public partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   public sealed class RegistrationBuilder(CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> owner) : ITeventHandlerRegistrar<TTevent>
   {
      readonly CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> _owner = owner;

      ///<summary>Registers a for any tevent that implements THandledTevent. All matching handlers will be called in the order they were registered.</summary>
      RegistrationBuilder For<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : TTevent => ForGenericTevent(handler);

      ///<summary>Lets you register handlers for tevent interfaces that may be defined outside of the tevent hierarchy you specify with TTevent.
      /// Useful for listening to generic tevents such as ITaggregateCreatedTevent or ITaggregateDeletedTevent
      /// Be aware that the concrete tevent received MUST still actually inherit TTevent or there will be an InvalidCastException
      /// </summary>
      RegistrationBuilder ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) where THandledTevent : ITevent
      {
         TessageTypeInspector.AssertValidForSubscription(typeof(THandledTevent));
         if(typeof(THandledTevent).Is<IPublisherIdentifyingTevent<ITevent>>()) throw new Exception($"Handlers of type {typeof(IPublisherIdentifyingTevent<>).Name} must be registered through the {nameof(ForWrapped)} method.");
         _owner._handlers.Add(new RegisteredHandler<THandledTevent>(handler));
         _owner._totalHandlers++;
         return this;
      }

      RegistrationBuilder ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherIdentifyingTevent<TTevent> => ForWrappedGeneric(handler);

      RegistrationBuilder ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) where TWrapperTevent : IPublisherIdentifyingTevent<ITevent>
      {
         TessageTypeInspector.AssertValidForSubscription(typeof(TWrapperTevent));
         _owner._handlers.Add(new RegisteredWrappedHandler<TWrapperTevent>(handler));
         _owner._totalHandlers++;
         return this;
      }

      RegistrationBuilder BeforeHandlers(Action<TTevent> runBeforeHandlers)
      {
         //Urgent: fix this. Use the registered handler classes above
         _owner._runBeforeHandlers.Add(e => runBeforeHandlers(((IPublisherIdentifyingTevent<TTevent>)e).Tevent));
         _owner._totalHandlers++;
         return this;
      }

      RegistrationBuilder AfterHandlers(Action<TTevent> runAfterHandlers)
      {
         //Urgent: fix this
         _owner._runAfterHandlers.Add(e => runAfterHandlers(((IPublisherIdentifyingTevent<TTevent>)e).Tevent));
         return this;
      }

      RegistrationBuilder IgnoreUnhandled<T>() where T : ITevent
      {
         _owner._ignoredTevents.Add(typeof(T));                //Urgent: Remove?
         _owner._ignoredTevents.Add(typeof(IPublisherIdentifyingTevent<T>)); //urgent: Is this correct?
         _owner._totalHandlers++;
         return this;
      }

      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.ForGenericTevent<THandledTevent>(Action<THandledTevent> handler) => ForGenericTevent(handler);
      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.BeforeHandlers<THandledTevent>(Action<THandledTevent> runBeforeHandlers) => BeforeHandlers(e => runBeforeHandlers((THandledTevent)e));
      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.AfterHandlers<THandledTevent>(Action<THandledTevent> runAfterHandlers) => AfterHandlers(e => runAfterHandlers((THandledTevent)e));
      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.IgnoreUnhandled<THandledTevent>() => IgnoreUnhandled<THandledTevent>();
      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.For<THandledTevent>(Action<THandledTevent> handler) => For(handler);
      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.ForWrapped<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrapped(handler);
      ITeventHandlerRegistrar<TTevent> ITeventHandlerRegistrar<TTevent>.ForWrappedGeneric<TWrapperTevent>(Action<TWrapperTevent> handler) => ForWrappedGeneric(handler);
   }
}
