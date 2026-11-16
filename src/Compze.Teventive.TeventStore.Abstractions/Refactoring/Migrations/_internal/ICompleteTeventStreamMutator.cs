using Compze.Teventive.Taggregates.Tevents;

namespace Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations._internal;

interface ICompleteTeventStreamMutator
{
   IEnumerable<ITaggregateTevent<ITaggregateTeventWithIdentity>> Mutate(IEnumerable<ITaggregateTevent<ITaggregateTeventWithIdentity>> teventStream);
}