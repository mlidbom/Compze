using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Internal;

///<summary>Implementations are responsible for mutating the events of one aggregate instance. Callers are required to call <see cref="Mutate"/> with each event in order and to end by calling <see cref="EndOfAggregate"/></summary>
interface ISingleAggregateInstanceEventStreamMutator
{
   IEnumerable<AggregateTevent> Mutate(AggregateTevent tevent);
   IEnumerable<AggregateTevent> EndOfAggregate();
}