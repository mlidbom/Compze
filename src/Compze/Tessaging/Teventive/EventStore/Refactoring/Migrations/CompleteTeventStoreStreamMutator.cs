using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

abstract class CompleteTeventStoreStreamMutator
{
   public static ICompleteTeventStreamMutator Create(IReadOnlyList<ITeventMigration> teventMigrationFactories) => teventMigrationFactories.Any()
                                                                                                                  ? new RealMutator(teventMigrationFactories)
                                                                                                                  : new OnlySerializeVersionsMutator();

   class OnlySerializeVersionsMutator : ICompleteTeventStreamMutator
   {
      readonly Dictionary<Guid, int> _aggregateVersions = new();

      public IEnumerable<AggregateTevent> Mutate(IEnumerable<AggregateTevent> teventStream)
      {
         foreach(var @tevent in teventStream)
         {
            var version = _aggregateVersions.GetOrAddDefault(@tevent.AggregateId) + 1;
            _aggregateVersions[@tevent.AggregateId] = version;
#pragma warning disable CS0618 // Type or member is obsolete
            ((IMutableAggregateTevent)@tevent).SetAggregateVersionInternal(version);
#pragma warning restore CS0618 // Type or member is obsolete
            yield return @tevent;
         }
      }
   }

   class RealMutator(IReadOnlyList<ITeventMigration> teventMigrationFactories) : ICompleteTeventStreamMutator
   {
      readonly IReadOnlyList<ITeventMigration> _teventMigrationFactories = teventMigrationFactories;
      readonly Dictionary<Guid, ISingleAggregateInstanceTeventStreamMutator> _aggregateMutatorsCache = new();

      public IEnumerable<AggregateTevent> Mutate(IEnumerable<AggregateTevent> teventStream)
      {
         foreach(var @tevent in teventStream)
         {
            var mutatedTevents = _aggregateMutatorsCache.GetOrAdd(
               @tevent.AggregateId,
               () => SingleAggregateInstanceTeventStreamMutator.Create(@tevent, _teventMigrationFactories)
            ).Mutate(@tevent);

            foreach(var mutatedTevent in mutatedTevents)
            {
               yield return mutatedTevent;
            }
         }

         // ReSharper disable once ForeachCanBePartlyConvertedToTueryUsingAnotherGetEnumerator
         foreach (var mutator in _aggregateMutatorsCache)
         {
            foreach (var finalTevent in mutator.Value.EndOfAggregate())
            {
               yield return finalTevent;
            }
         }
      }
   }
}