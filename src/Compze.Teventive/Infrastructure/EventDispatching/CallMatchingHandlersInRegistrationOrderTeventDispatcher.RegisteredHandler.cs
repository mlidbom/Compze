// ReSharper disable ForCanBeConvertedToForeach this file needs these optimizations...

using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Teventive.Infrastructure.EventDispatching;

/// <summary>
/// Calls all matching handlers in the order they were registered when an tevent is Dispatched.
/// Handlers should be registered using the RegisterHandlers method in the constructor of the inheritor.
/// </summary>
partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   public abstract class RegisteredHandler
   {
      internal abstract Action<ITevent>? TryCreateHandlerFor(Type teventType);
   }

   public class RegisteredHandler<THandledTevent>(Action<THandledTevent> handler) : RegisteredHandler where THandledTevent : ITevent
   {
      //Since handler has specified no preference for wrapper type the most generic of all will do and any wrapped tevent containing a matching tevent should be dispatched to this handler.
      readonly Action<THandledTevent> _handler = handler;

      internal override Action<ITevent>? TryCreateHandlerFor(Type teventType)
      {
         if(typeof(THandledTevent).IsAssignableFrom(teventType))
         {
            return tevent => _handler((THandledTevent)tevent);
         } else if(teventType.Is<IPublisherIdentifyingTevent<THandledTevent>>())
         {
            return tevent => _handler(((IPublisherIdentifyingTevent<THandledTevent>)tevent).Tevent);
         } else
         {
            return null;
         }
      }
   }

   public class RegisteredWrappedHandler<THandledWrapperTevent>(Action<THandledWrapperTevent> handler) : RegisteredHandler where THandledWrapperTevent : IPublisherIdentifyingTevent<ITevent>
   {
      readonly Action<THandledWrapperTevent> _handler = handler;

      internal override Action<ITevent>? TryCreateHandlerFor(Type teventType) =>
         typeof(THandledWrapperTevent).IsAssignableFrom(teventType)
            ? tevent => _handler((THandledWrapperTevent)tevent)
            : null;
   }
}
