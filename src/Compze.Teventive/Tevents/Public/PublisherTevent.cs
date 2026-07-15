using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Teventive.Tevents.Public;

///<summary>Wraps tevents that are published without a publisher-identifying wrapper: every tevent is wrapped before routing,<br/>
/// and a tevent dispatched unwrapped is wrapped by <see cref="WrapTevent{TTevent}"/> in a <see cref="PublisherTevent{TTevent}"/> closed over its runtime type.</summary>
public static class PublisherTevent
{
   ///<summary>Wraps <paramref name="tevent"/> in a <see cref="PublisherTevent{TTevent}"/> closed over its runtime type,<br/>
   /// so that the wrapper is assignable to <see cref="IPublisherTevent{TTevent}"/> of every tevent type the wrapped tevent itself is assignable to.</summary>
   public static IPublisherTevent<TTevent> WrapTevent<TTevent>(TTevent tevent) where TTevent : class, ITevent =>
      (IPublisherTevent<TTevent>)Constructor.ForGenericType(typeof(PublisherTevent<>))
                                            .WithArgument(tevent.GetType())
                                            .Invoke(tevent);

   ///<summary>The tevent in its wrapped form: an already-wrapped tevent passes through as it stands; anything else is wrapped by <see cref="WrapTevent{TTevent}"/>.<br/>
   /// Every tevent is wrapped before routing - a boundary that receives a tevent that may or may not already be wrapped normalizes here.</summary>
   public static IPublisherTevent<ITevent> Wrapped(ITevent tevent) => tevent as IPublisherTevent<ITevent> ?? WrapTevent(tevent);

   ///<summary>Wraps <paramref name="tevent"/> in <paramref name="wrapperTeventImplementation"/>'s generic type definition closed over the tevent's runtime type,<br/>
   /// so that the wrapper is assignable to the wrapper interface of every tevent type the wrapped tevent itself is assignable to.<br/>
   /// This is the one home of publisher-declared wrapping: a taggregate wrapping in its declared wrapper implementation and a shared tomponent's slot<br/>
   /// wrapping in its adopting wrapper tevent both close their wrapper type here.</summary>
   public static IPublisherTevent<ITevent> WrapIn(Type wrapperTeventImplementation, ITevent tevent) =>
      (IPublisherTevent<ITevent>)Constructor.ForGenericType(wrapperTeventImplementation)
                                            .WithArgument(tevent.GetType())
                                            .Invoke(tevent);

   ///<summary>The wrapper type <see cref="WrapTevent{TTevent}"/> produces for a tevent of <paramref name="teventType"/>: <see cref="PublisherTevent{TTevent}"/> closed over it.</summary>
   public static Type WrapperTypeFor(Type teventType) => typeof(PublisherTevent<>).MakeGenericType(teventType);

   ///<summary>The wrapper type every wrapping of <paramref name="teventType"/> is assignable to: <see cref="IPublisherTevent{TTevent}"/> closed over it.<br/>
   /// This is the one translation rule of the routing model: subscribing to, filtering by, or ignoring an inner tevent type means matching every<br/>
   /// <see cref="IPublisherTevent{TTevent}"/> of it. A type that is already a wrapper type passes through unchanged.</summary>
   public static Type WrapperTypeMatchingAllWrappingsOf(Type teventType) =>
      teventType.Is<IPublisherTevent<ITevent>>() ? teventType : typeof(IPublisherTevent<>).MakeGenericType(teventType);
}

///<summary>The root implementation of <see cref="IPublisherTevent{TTevent}"/>: a wrapper that carries the wrapped tevent's full type information<br/>
/// but identifies no publisher beyond it. Publisher-specific wrappers implement more derived wrapper interfaces; this is what a tevent is wrapped in when<br/>
/// its publisher declares none, via <see cref="PublisherTevent.WrapTevent{TTevent}"/>.</summary>
public class PublisherTevent<TTevent>(TTevent tevent) : IPublisherTevent<TTevent>
   where TTevent : ITevent
{
   public TTevent Tevent { get; } = tevent;
}
