using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Eventstore.Public;

namespace Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;

interface ICompleteEventStreamMutator
{
   IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream);
}