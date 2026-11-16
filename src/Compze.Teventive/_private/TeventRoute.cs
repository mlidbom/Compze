using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive._private;

///<summary>One route of a <see cref="TeventRouting"/>: the wrapper tevent types it matches, and what it does with a matching tevent on an instance.</summary>
sealed class TeventRoute(Type matchesWrappersAssignableTo, Action<object, IPublisherTevent<ITevent>> deliver)
{
   readonly Type _matchesWrappersAssignableTo = matchesWrappersAssignableTo;
   internal Action<object, IPublisherTevent<ITevent>> Deliver { get; } = deliver;
   internal bool Matches(Type wrapperTeventType) => _matchesWrappersAssignableTo.IsAssignableFrom(wrapperTeventType);
}
