using System.Collections.Generic;

namespace Compze.Persistence.EventStore.Refactoring.Migrations;

interface ICompleteEventStreamMutator
{
   IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream);
}