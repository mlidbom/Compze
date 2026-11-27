using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation._internal;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

///<summary>One participation handler of tevents, as the <see cref="TessageHandlerRoster"/> holds it: the handler, with the wrapper<br/>
/// routing key its subscription translates to, and its <see cref="HandlingOrder"/>. A published tevent runs it when the tevent's<br/>
/// wrapper type is compatible with that key.</summary>
///<remarks>Routing operates on wrapper types only, so a subscription to an inner tevent type is keyed under the wrapper type matching<br/>
/// every wrapping of it (<see cref="PublisherTevent.WrapperTypeMatchingAllWrappingsOf"/>) and receives the unwrapped tevent, while a<br/>
/// subscription to a wrapper type is keyed as it stands and receives the wrapper: publisher-conscious subscription.</remarks>
sealed class TeventHandlerRegistration
{
   readonly Type _subscribedWrapperType;

   TeventHandlerRegistration(Type subscribedWrapperType, Func<ITevent, IUnitOfWorkResolver, CancellationToken, Task> handle, HandlingOrder order)
   {
      _subscribedWrapperType = subscribedWrapperType;
      Handle = handle;
      Order = order;
   }

   ///<summary>The registration of <paramref name="handler"/>, for tevents compatible with <typeparamref name="TTevent"/>.</summary>
   internal static TeventHandlerRegistration For<TTevent>(Func<TTevent, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order) where TTevent : ITevent
   {
      TessageInspector.AssertValidForSubscription<TTevent>();
      return new TeventHandlerRegistration(PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent)),
                                           typeof(TTevent).Is<IPublisherTevent<ITevent>>()
                                              ? (wrappedTevent, unitOfWork, cancellationToken) => handler((TTevent)wrappedTevent, unitOfWork, cancellationToken)
                                              : (wrappedTevent, unitOfWork, cancellationToken) => handler((TTevent)((IPublisherTevent<ITevent>)wrappedTevent).Tevent, unitOfWork, cancellationToken),
                                           order);
   }

   ///<summary>Runs the handler on a wrapped tevent: on the wrapper itself for a subscription to a wrapper type, and on the tevent it<br/>
   /// wraps otherwise.</summary>
   internal Func<ITevent, IUnitOfWorkResolver, CancellationToken, Task> Handle { get; }

   ///<summary>Whether the tevents it handles keep their place in the order the endpoint's inbox handles tessages in, as far as<br/>
   /// this handler decides: a tevent gives it up only when every handler it runs gives it up.</summary>
   internal HandlingOrder Order { get; }

   ///<summary>Whether a tevent whose wrapper is of <paramref name="wrapperTeventType"/> runs this handler: whether the wrapper is<br/>
   /// compatible with the subscription's wrapper type.</summary>
   internal bool MatchesTeventsWrappedAs(Type wrapperTeventType) => _subscribedWrapperType.IsAssignableFrom(wrapperTeventType);
}
