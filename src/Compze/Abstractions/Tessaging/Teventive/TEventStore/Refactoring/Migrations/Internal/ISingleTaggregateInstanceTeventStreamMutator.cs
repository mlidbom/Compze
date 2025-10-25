using System.Collections.Generic;
using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TEventStore.Refactoring.Migrations.Internal;

///<summary>Implementations are responsible for mutating the tevents of one taggregate instance. Callers are required to call <see cref="Mutate"/> with each tevent in order and to end by calling <see cref="EndOfTaggregate"/></summary>
interface ISingleTaggregateInstanceTeventStreamMutator
{
   IEnumerable<TaggregateTevent> Mutate(TaggregateTevent tevent);
   IEnumerable<TaggregateTevent> EndOfTaggregate();
}