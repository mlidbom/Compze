using System.Collections.Generic;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Core.Tessaging.Teventive.TEventStore.Refactoring.Migrations.Internal;

interface ICompleteTeventStreamMutator
{
   IEnumerable<TaggregateTevent> Mutate(IEnumerable<TaggregateTevent> teventStream);
}