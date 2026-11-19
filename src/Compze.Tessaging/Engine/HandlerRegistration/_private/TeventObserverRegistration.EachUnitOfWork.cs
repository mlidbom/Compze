using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

abstract partial class TeventObserverRegistration
{
   ///<summary>An observer called once for each committed unit of work that published tevents its subscription matches, with<br/>
   /// those tevents as one observation. Never called with an empty list.</summary>
   sealed class EachUnitOfWork : TeventObserverRegistration
   {
      readonly Func<IReadOnlyList<IPublisherTevent<ITevent>>, IScopeResolver, Task> _observeAsync;

      internal EachUnitOfWork(Type subscribedTeventType, Func<IReadOnlyList<IPublisherTevent<ITevent>>, IScopeResolver, Task> observeAsync) : base(subscribedTeventType) =>
         _observeAsync = observeAsync;

      internal override IEnumerable<IReadOnlyList<IPublisherTevent<ITevent>>> ObservationsOf(IReadOnlyList<IPublisherTevent<ITevent>> committedTevents)
      {
         IReadOnlyList<IPublisherTevent<ITevent>> observed = [..ObservedOf(committedTevents)];
         return observed.Count > 0 ? [observed] : [];
      }

      internal override Task ObserveAsync(IReadOnlyList<IPublisherTevent<ITevent>> observation, IScopeResolver scope) => _observeAsync(observation, scope);
   }
}
