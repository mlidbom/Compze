using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive._private;

///<summary>One route of a <see cref="TeventRouting"/>: the wrapper tevent types it matches, and what it does with a matching tevent on an instance.</summary>
///<param name="matchesWrappersAssignableTo">The route matches a tevent whose wrapper is assignable to this type.</param>
///<param name="deliver">What the route does with a matching tevent, on an instance of the routing's class.</param>
///<param name="partTevent">The tevent type of the part the route routes to, a component or an entity collection; null for a route to an applier or a slot.</param>
sealed class TeventRoute(Type matchesWrappersAssignableTo, Action<object, IPublisherTevent<ITevent>> deliver, Type? partTevent = null)
{
   readonly Type _matchesWrappersAssignableTo = matchesWrappersAssignableTo;
   internal Action<object, IPublisherTevent<ITevent>> Deliver { get; } = deliver;
   internal Type? PartTevent { get; } = partTevent;
   internal bool Matches(Type wrapperTeventType) => _matchesWrappersAssignableTo.IsAssignableFrom(wrapperTeventType);
}
