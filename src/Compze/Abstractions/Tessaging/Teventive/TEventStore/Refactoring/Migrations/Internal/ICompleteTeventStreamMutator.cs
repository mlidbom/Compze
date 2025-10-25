using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Refactoring.Migrations.Internal;

interface ICompleteTeventStreamMutator
{
   IEnumerable<TaggregateTevent> Mutate(IEnumerable<TaggregateTevent> teventStream);
}