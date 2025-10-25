using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;

interface ICompleteTeventStreamMutator
{
   IEnumerable<AggregateTevent> Mutate(IEnumerable<AggregateTevent> teventStream);
}