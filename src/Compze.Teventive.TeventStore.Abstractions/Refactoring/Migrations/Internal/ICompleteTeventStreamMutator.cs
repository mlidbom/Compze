using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;

public interface ICompleteTeventStreamMutator
{
   IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(IEnumerable<ITaggregateTevent<ITaggregateTevent>> teventStream);
}