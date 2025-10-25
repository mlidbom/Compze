using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;
using Compze.Abstractions.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Utilities.Contracts;
using Compze.Utilities.SystemCE.LinqCE;

// ReSharper disable ForCanBeConvertedToForeach

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

//Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
//What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
//The performance of this class is extremely important since it is called at least once for every tevent that is loaded from the tevent store when you have any migrations activated. It is called A LOT.
//This is one of those central classes for which optimization is actually vitally important.
//Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
class SingleAggregateInstanceTeventStreamMutator : ISingleAggregateInstanceTeventStreamMutator
{
   readonly Guid _aggregateId;
   readonly ISingleAggregateInstanceHandlingTeventMigrator[] _teventMigrators;
   readonly TeventModifier _teventModifier;

   int _aggregateVersion = 1;

   public static ISingleAggregateInstanceTeventStreamMutator Create(IAggregateTevent creationTevent, IReadOnlyList<ITeventMigration> teventMigrations, Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback = null)
      => new SingleAggregateInstanceTeventStreamMutator(creationTevent, teventMigrations, teventsAddedCallback);

   SingleAggregateInstanceTeventStreamMutator
      (IAggregateTevent creationTevent, IEnumerable<ITeventMigration> teventMigrations, Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback)
   {
      _teventModifier = new TeventModifier(teventsAddedCallback ?? (_ => { }));
      _aggregateId = creationTevent.AggregateId;
      _teventMigrators = teventMigrations
                       .Where(migration => migration.MigratedAggregateTeventHierarchyRootInterface.IsInstanceOfType(creationTevent))
                       .Select(migration => migration.CreateSingleAggregateInstanceHandlingMigrator())
                       .ToArray();
   }

   static IEnumerable<AggregateTevent> SingleTeventSequence(AggregateTevent tevent) { yield return tevent; }
   public IEnumerable<AggregateTevent> Mutate(AggregateTevent tevent)
   {
      Assert.Argument.Is(_aggregateId == tevent.AggregateId);
      if (_teventMigrators.Length == 0)
      {
         return SingleTeventSequence(tevent);
      }

#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableAggregateTevent)tevent).SetAggregateVersionInternal(_aggregateVersion);
#pragma warning restore CS0618 // Type or member is obsolete
      _teventModifier.Reset(tevent);

      for(var index = 0; index < _teventMigrators.Length; index++)
      {
         if (_teventModifier.Tevents == null)
         {
            _teventMigrators[index].MigrateTevent(tevent, _teventModifier);
         }
         else
         {
            var node = _teventModifier.Tevents.First;
            while (node != null)
            {
               _teventModifier.MoveTo(node);
               _teventMigrators[index].MigrateTevent(node.Value, _teventModifier);
               node = node.Next;
            }
         }
      }

      var newHistory = _teventModifier.MutatedHistory;
      _aggregateVersion += newHistory.Length;
      return newHistory;
   }

   public IEnumerable<AggregateTevent> EndOfAggregate()
   {
      return EnumerableCE.Create(new EndOfAggregateHistoryTeventPlaceHolder(_aggregateId, _aggregateVersion))
                         .SelectMany(Mutate)
                         .Where(@tevent => @tevent.GetType() != typeof(EndOfAggregateHistoryTeventPlaceHolder));
   }

   public static AggregateTevent[] MutateCompleteAggregateHistory
   (IReadOnlyList<ITeventMigration> teventMigrations,
    AggregateTevent[] tevents,
    Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback = null)
   {
      if (teventMigrations.None())
      {
         return tevents;
      }

      if(tevents.None())
      {
         return Enumerable.Empty<AggregateTevent>().ToArray();
      }

      var mutator = Create(tevents.First(), teventMigrations, teventsAddedCallback);

      var result = tevents
                  .SelectMany(mutator.Mutate)
                  .Concat(mutator.EndOfAggregate())
                  .ToArray();

      AssertMigrationsAreIdempotent(teventMigrations, result);
      AggregateHistoryValidator.ValidateHistory(result.First().AggregateId, result);

      return result;
   }

   public static void AssertMigrationsAreIdempotent(IReadOnlyList<ITeventMigration> teventMigrations, AggregateTevent[] tevents)
   {
      var creationTevent = tevents.First();

      var migrators = teventMigrations
                     .Where(migration => migration.MigratedAggregateTeventHierarchyRootInterface.IsInstanceOfType(creationTevent))
                     .Select(migration => migration.CreateSingleAggregateInstanceHandlingMigrator())
                     .ToArray();

      for(var teventIndex = 0; teventIndex < tevents.Length; teventIndex++)
      {
         var @tevent = tevents[teventIndex];
         for(var migratorIndex = 0; migratorIndex < migrators.Length; migratorIndex++)
         {
            migrators[migratorIndex].MigrateTevent(@tevent, AssertMigrationsAreIdempotentTeventModifier.Instance);
         }
      }
   }
}

sealed class EndOfAggregateHistoryTeventPlaceHolder : AggregateTevent {
#pragma warning disable CS0618 // Type or member is obsolete
    public EndOfAggregateHistoryTeventPlaceHolder(Guid aggregateId, int i):base(aggregateId) => ((IMutableAggregateTevent)this).SetAggregateVersionInternal(i);
#pragma warning restore CS0618 // Type or member is obsolete
}