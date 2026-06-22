using Compze.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;

///<summary>Implementations are responsible for mutating the tevents of one taggregate instance. Callers are required to call <see cref="Mutate"/> with each tevent in order and to end by calling <see cref="EndOfTaggregate"/></summary>
public interface ISingleTaggregateInstanceTeventStreamMutator
{
   IEnumerable<TaggregateTevent> Mutate(TaggregateTevent tevent);
   IEnumerable<TaggregateTevent> EndOfTaggregate();
}