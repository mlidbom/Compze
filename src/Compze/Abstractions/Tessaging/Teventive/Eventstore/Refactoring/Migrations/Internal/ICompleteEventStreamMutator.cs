using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Internal;

interface ICompleteEventStreamMutator
{
   IEnumerable<AggregateEvent> Mutate(IEnumerable<AggregateEvent> eventStream);
}