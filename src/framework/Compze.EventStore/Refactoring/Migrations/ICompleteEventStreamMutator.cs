using Compze.EventStore.Abstractions;
using System.Collections.Generic;

namespace Compze.EventStore.Refactoring.Migrations;

interface ICompleteEventStreamMutator
{
   IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream);
}