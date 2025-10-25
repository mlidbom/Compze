using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.EventStore.Refactoring.Migrations.Internal;

interface ICompleteEventStreamMutator
{
   IEnumerable<AggregateTevent> Mutate(IEnumerable<AggregateTevent> eventStream);
}