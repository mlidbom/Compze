using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tessaging.Validation._internal;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

///<summary>One participation handler of tevents, as the <see cref="TessageHandlerRoster"/> holds it: the handler, with the wrapper<br/>
/// routing key its subscription translates to. A published tevent runs it when the tevent's wrapper type is compatible with that key.</summary>
///<remarks>Routing operates on wrapper types only, so a subscription to an inner tevent type is keyed under the wrapper type matching<br/>
/// every wrapping of it (<see cref="PublisherTevent.WrapperTypeMatchingAllWrappingsOf"/>) and receives the unwrapped tevent, while a<br/>
/// subscription to a wrapper type is keyed as it stands and receives the wrapper: publisher-conscious subscription.</remarks>
sealed class TeventHandlerRegistration
{
   readonly Type _subscribedWrapperType;

   TeventHandlerRegistration(Type subscribedWrapperType, Func<ITevent, IUnitOfWorkResolver, Task> handle)
   {
      _subscribedWrapperType = subscribedWrapperType;
      Handle = handle;
   }

   ///<summary>The registration of <paramref name="handler"/>, for tevents compatible with <typeparamref name="TTevent"/>.</summary>
   internal static TeventHandlerRegistration For<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler) where TTevent : ITevent
   {
      TessageInspector.AssertValidForSubscription<TTevent>();
      return new TeventHandlerRegistration(PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent)),
                                           typeof(TTevent).Is<IPublisherTevent<ITevent>>()
                                              ? (wrappedTevent, unitOfWork) => handler((TTevent)wrappedTevent, unitOfWork)
                                              : (wrappedTevent, unitOfWork) => handler((TTevent)((IPublisherTevent<ITevent>)wrappedTevent).Tevent, unitOfWork));
   }

   ///<summary>Runs the handler on a wrapped tevent: on the wrapper itself for a subscription to a wrapper type, and on the tevent it<br/>
   /// wraps otherwise.</summary>
   internal Func<ITevent, IUnitOfWorkResolver, Task> Handle { get; }

   ///<summary>Whether a tevent whose wrapper is of <paramref name="wrapperTeventType"/> runs this handler: whether the wrapper is<br/>
   /// compatible with the subscription's wrapper type.</summary>
   internal bool MatchesTeventsWrappedAs(Type wrapperTeventType) => _subscribedWrapperType.IsAssignableFrom(wrapperTeventType);
}
