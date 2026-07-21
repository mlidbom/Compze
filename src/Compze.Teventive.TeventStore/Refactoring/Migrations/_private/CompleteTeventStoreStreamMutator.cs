using Compze.Abstractions.Public;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations._internal;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;

namespace Compze.Teventive.TeventStore.Refactoring.Migrations._private;

abstract class CompleteTeventStoreStreamMutator
{
   public static ICompleteTeventStreamMutator Create(IReadOnlyList<ITeventMigration> teventMigrationFactories) => teventMigrationFactories.Any()
                                                                                                                  ? new RealMutator(teventMigrationFactories)
                                                                                                                  : new OnlySerializeVersionsMutator();

   class OnlySerializeVersionsMutator : ICompleteTeventStreamMutator
   {
      readonly Dictionary<TaggregateId, int> _taggregateVersions = new();

      public IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(IEnumerable<ITaggregateTevent<ITaggregateTevent>> teventStream)
      {
         foreach(var wrappedTevent in teventStream)
         {
            var version = _taggregateVersions.GetOrAddDefault(wrappedTevent.Tevent.TaggregateId) + 1;
            _taggregateVersions[wrappedTevent.Tevent.TaggregateId] = version;
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetTaggregateVersionInternal(version);
#pragma warning restore CS0618 // Type or member is obsolete
            yield return wrappedTevent;
         }
      }
   }

   class RealMutator(IReadOnlyList<ITeventMigration> teventMigrationFactories) : ICompleteTeventStreamMutator
   {
      readonly IReadOnlyList<ITeventMigration> _teventMigrationFactories = teventMigrationFactories;
      readonly Dictionary<TaggregateId, ISingleTaggregateInstanceTeventStreamMutator> _taggregateMutatorsCache = new();

      public IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(IEnumerable<ITaggregateTevent<ITaggregateTevent>> teventStream)
      {
         foreach(var wrappedTevent in teventStream)
         {
            var mutatedTevents = _taggregateMutatorsCache.GetOrAdd(
               wrappedTevent.Tevent.TaggregateId,
               () => SingleTaggregateInstanceTeventStreamMutator.Create(wrappedTevent, _teventMigrationFactories)
            ).Mutate(wrappedTevent);

            foreach(var mutatedTevent in mutatedTevents)
            {
               yield return mutatedTevent;
            }
         }

         // ReSharper disable once ForeachCanBePartlyConvertedToTueryUsingAnotherGetEnumerator
         foreach (var mutator in _taggregateMutatorsCache)
         {
            foreach (var finalTevent in mutator.Value.EndOfTaggregate())
            {
               yield return finalTevent;
            }
         }
      }
   }
}
