using System.Collections.Generic;
using Compze.Tessaging.Teventive.EventStore.Abstractions;

namespace Compze.Tessaging.Teventive.EventStore.Refactoring.Migrations;

interface ICompleteEventStreamMutator
{
   IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream);
}