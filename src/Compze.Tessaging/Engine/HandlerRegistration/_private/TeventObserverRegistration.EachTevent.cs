using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

abstract partial class TeventObserverRegistration
{
   ///<summary>An observer called once for each tevent its subscription matches: each observation holds one tevent.</summary>
   sealed class EachTevent : TeventObserverRegistration
   {
      readonly Func<IPublisherTevent<ITevent>, IScopeResolver, Task> _observeAsync;

      internal EachTevent(Type subscribedTeventType, Func<IPublisherTevent<ITevent>, IScopeResolver, Task> observeAsync) : base(subscribedTeventType) =>
         _observeAsync = observeAsync;

      internal override IEnumerable<IReadOnlyList<IPublisherTevent<ITevent>>> ObservationsOf(IReadOnlyList<IPublisherTevent<ITevent>> committedTevents) =>
         ObservedOf(committedTevents).Select(IReadOnlyList<IPublisherTevent<ITevent>> (it) => [it]);

      internal override Task ObserveAsync(IReadOnlyList<IPublisherTevent<ITevent>> observation, IScopeResolver scope) => _observeAsync(observation.Single(), scope);
   }
}
