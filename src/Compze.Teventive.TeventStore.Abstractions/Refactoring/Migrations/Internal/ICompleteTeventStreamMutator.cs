using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;

public interface ICompleteTeventStreamMutator
{
   IEnumerable<ITaggregateIdentifyingTevent<ITaggregateTevent>> Mutate(IEnumerable<ITaggregateIdentifyingTevent<ITaggregateTevent>> teventStream);
}