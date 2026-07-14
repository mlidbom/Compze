using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Internal;

public interface ICompleteTeventStreamMutator
{
   IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(IEnumerable<ITaggregateTevent<ITaggregateTevent>> teventStream);
}