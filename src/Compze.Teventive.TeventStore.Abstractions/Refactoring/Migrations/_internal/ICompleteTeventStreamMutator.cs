using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations._internal;

interface ICompleteTeventStreamMutator
{
   IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(IEnumerable<ITaggregateTevent<ITaggregateTevent>> teventStream);
}