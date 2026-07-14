using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Internal;

///<summary>Implementations are responsible for mutating the tevents of one taggregate instance. Callers are required to call <see cref="Mutate"/> with each tevent in order and to end by calling <see cref="EndOfTaggregate"/></summary>
public interface ISingleTaggregateInstanceTeventStreamMutator
{
   IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(ITaggregateTevent<ITaggregateTevent> wrappedTevent);
   IEnumerable<ITaggregateTevent<ITaggregateTevent>> EndOfTaggregate();
}