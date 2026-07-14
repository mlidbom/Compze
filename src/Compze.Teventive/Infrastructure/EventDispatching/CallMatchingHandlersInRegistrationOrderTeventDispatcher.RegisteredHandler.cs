using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Teventive.Infrastructure.EventDispatching;

partial class CallMatchingHandlersInRegistrationOrderTeventDispatcher<TTevent> where TTevent : class, ITevent
{
   ///<summary>A single subscription's routing rule. Routing operates exclusively on wrapper types: given the type of a dispatched<br/>
   /// <see cref="IPublisherTevent{TTevent}"/>, an implementation either produces the action that delivers the tevent to its handler, or null when the subscription does not match.</summary>
   public abstract class RegisteredHandler
   {
      internal abstract Action<ITevent>? TryCreateHandlerFor(Type wrapperTeventType);
   }

   ///<summary>A subscription to an inner tevent type: matches every wrapper of a tevent assignable to <typeparamref name="THandledTevent"/> —<br/>
   /// by the wrapper interface's covariance that is exactly assignability to <see cref="IPublisherTevent{TTevent}"/> of <typeparamref name="THandledTevent"/> —<br/>
   /// and unwraps at delivery: the handler receives the inner <see cref="IPublisherTevent{TTevent}.Tevent"/>.</summary>
   public class RegisteredHandler<THandledTevent>(Action<THandledTevent> handler) : RegisteredHandler where THandledTevent : ITevent
   {
      readonly Action<THandledTevent> _handler = handler;

      internal override Action<ITevent>? TryCreateHandlerFor(Type wrapperTeventType) =>
         wrapperTeventType.Is<IPublisherTevent<THandledTevent>>()
            ? tevent => _handler(((IPublisherTevent<THandledTevent>)tevent).Tevent)
            : null;
   }

   ///<summary>A subscription to a wrapper tevent type — publisher-conscious: matches by assignability of the wrapper type itself,<br/>
   /// so only tevents wrapped by a matching publisher qualify, and delivers the wrapper unopened.</summary>
   public class RegisteredWrappedHandler<THandledWrapperTevent>(Action<THandledWrapperTevent> handler) : RegisteredHandler where THandledWrapperTevent : IPublisherTevent<ITevent>
   {
      readonly Action<THandledWrapperTevent> _handler = handler;

      internal override Action<ITevent>? TryCreateHandlerFor(Type wrapperTeventType) =>
         typeof(THandledWrapperTevent).IsAssignableFrom(wrapperTeventType)
            ? tevent => _handler((THandledWrapperTevent)tevent)
            : null;
   }
}
