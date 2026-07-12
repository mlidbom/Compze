using System.Linq.Expressions;
using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;

namespace Compze.Teventive.Tevents.Public;

///<summary>Wraps tevents that are published without a publisher-identifying wrapper: every tevent is wrapped before routing,<br/>
/// and a tevent dispatched unwrapped is wrapped by <see cref="WrapTevent{TTevent}"/> in a <see cref="PublisherIdentifyingTevent{TTevent}"/> closed over its runtime type.</summary>
public static class PublisherIdentifyingTevent
{
   static IReadOnlyDictionary<Type, Func<ITevent, IPublisherIdentifyingTevent<ITevent>>> _wrapperConstructors = new Dictionary<Type, Func<ITevent, IPublisherIdentifyingTevent<ITevent>>>();
   static readonly IMonitor Monitor = IMonitor.New();

   ///<summary>Wraps <paramref name="tevent"/> in a <see cref="PublisherIdentifyingTevent{TTevent}"/> closed over its runtime type,<br/>
   /// so that the wrapper is assignable to <see cref="IPublisherIdentifyingTevent{TTevent}"/> of every tevent type the wrapped tevent itself is assignable to.</summary>
   public static IPublisherIdentifyingTevent<TTevent> WrapTevent<TTevent>(TTevent tevent) where TTevent : class, ITevent =>
      (IPublisherIdentifyingTevent<TTevent>)ConstructorFor(tevent.GetType()).Invoke(tevent);

   ///<summary>The wrapper type <see cref="WrapTevent{TTevent}"/> produces for a tevent of <paramref name="teventType"/>: <see cref="PublisherIdentifyingTevent{TTevent}"/> closed over it.</summary>
   public static Type WrapperTypeFor(Type teventType) => typeof(PublisherIdentifyingTevent<>).MakeGenericType(teventType);

   ///<summary>The wrapper type every wrapping of <paramref name="teventType"/> is assignable to: <see cref="IPublisherIdentifyingTevent{TTevent}"/> closed over it.<br/>
   /// This is the one translation rule of the routing model: subscribing to, filtering by, or ignoring an inner tevent type means matching every<br/>
   /// <see cref="IPublisherIdentifyingTevent{TTevent}"/> of it. A type that is already a wrapper type passes through unchanged.</summary>
   public static Type WrapperTypeMatchingAllWrappingsOf(Type teventType) =>
      teventType.Is<IPublisherIdentifyingTevent<ITevent>>() ? teventType : typeof(IPublisherIdentifyingTevent<>).MakeGenericType(teventType);

   static Func<ITevent, IPublisherIdentifyingTevent<ITevent>> ConstructorFor(Type teventType) =>
      Monitor.DoubleCheckedLocking(
         tryRead: () => _wrapperConstructors.GetValueOrDefault(teventType),
         field: ref _wrapperConstructors,
         createUpdatedFieldValue: () => _wrapperConstructors.AddToCopy(teventType, CreateConstructorFor(teventType)));

   //todo: use our Compze.Internals.SystemCE.ReflectionCE.Constructor
   static Func<ITevent, IPublisherIdentifyingTevent<ITevent>> CreateConstructorFor(Type teventType)
   {
      var wrapperImplementationType = typeof(PublisherIdentifyingTevent<>).MakeGenericType(teventType);
      var constructor = wrapperImplementationType.GetConstructor([teventType])._assert().NotNull();

      var teventParameter = Expression.Parameter(typeof(ITevent), "tevent");
      var constructorCall = Expression.New(constructor, Expression.Convert(teventParameter, teventType));
      return Expression.Lambda<Func<ITevent, IPublisherIdentifyingTevent<ITevent>>>(constructorCall, teventParameter).Compile();
   }
}

///<summary>The root implementation of <see cref="IPublisherIdentifyingTevent{TTevent}"/>: a wrapper that carries the wrapped tevent's full type information<br/>
/// but identifies no publisher beyond it. Publisher-specific wrappers implement more derived wrapper interfaces; this is what a tevent is wrapped in when<br/>
/// its publisher declares none, via <see cref="PublisherIdentifyingTevent.WrapTevent{TTevent}"/>.</summary>
public class PublisherIdentifyingTevent<TTevent>(TTevent tevent) : IPublisherIdentifyingTevent<TTevent>
   where TTevent : ITevent
{
   public TTevent Tevent { get; } = tevent;
}
