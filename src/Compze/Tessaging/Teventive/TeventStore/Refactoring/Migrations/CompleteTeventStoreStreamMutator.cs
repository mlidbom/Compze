using System.Collections.Generic;
using System.Linq;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

abstract class CompleteTeventStoreStreamMutator
{
   public static ICompleteTeventStreamMutator Create(IReadOnlyList<ITeventMigration> teventMigrationFactories) => teventMigrationFactories.Any()
                                                                                                                  ? new RealMutator(teventMigrationFactories)
                                                                                                                  : new OnlySerializeVersionsMutator();

   class OnlySerializeVersionsMutator : ICompleteTeventStreamMutator
   {
      readonly Dictionary<TaggregateId, int> _taggregateVersions = new();

      public IEnumerable<TaggregateTevent> Mutate(IEnumerable<TaggregateTevent> teventStream)
      {
         foreach(var tevent in teventStream)
         {
            var version = _taggregateVersions.GetOrAddDefault(tevent.TaggregateId) + 1;
            _taggregateVersions[tevent.TaggregateId] = version;
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(version);
#pragma warning restore CS0618 // Type or member is obsolete
            yield return tevent;
         }
      }
   }

   class RealMutator(IReadOnlyList<ITeventMigration> teventMigrationFactories) : ICompleteTeventStreamMutator
   {
      readonly IReadOnlyList<ITeventMigration> _teventMigrationFactories = teventMigrationFactories;
      readonly Dictionary<TaggregateId, ISingleTaggregateInstanceTeventStreamMutator> _taggregateMutatorsCache = new();

      public IEnumerable<TaggregateTevent> Mutate(IEnumerable<TaggregateTevent> teventStream)
      {
         foreach(var tevent in teventStream)
         {
            var mutatedTevents = _taggregateMutatorsCache.GetOrAdd(
               tevent.TaggregateId,
               () => SingleTaggregateInstanceTeventStreamMutator.Create(tevent, _teventMigrationFactories)
            ).Mutate(tevent);

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