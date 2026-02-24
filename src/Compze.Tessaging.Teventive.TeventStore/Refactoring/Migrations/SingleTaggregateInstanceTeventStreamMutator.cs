using System;
using System.Collections.Generic;
using System.Linq;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Internal;
using Compze.Core.Tessaging.Teventive.TeventStore.Refactoring.Migrations.Public;
using Compze.Contracts;
using Compze.Utilities.SystemCE.LinqCE;

// ReSharper disable ForCanBeConvertedToForeach

namespace Compze.Tessaging.Teventive.TeventStore.Refactoring.Migrations;

//Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
//What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
//The performance of this class is extremely important since it is called at least once for every tevent that is loaded from the tevent store when you have any migrations activated. It is called A LOT.
//This is one of those central classes for which optimization is actually vitally important.
//Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
public class SingleTaggregateInstanceTeventStreamMutator : ISingleTaggregateInstanceTeventStreamMutator
{
   readonly TaggregateId _taggregateId;
   readonly ISingleTaggregateInstanceHandlingTeventMigrator[] _teventMigrators;
   readonly TeventModifier _teventModifier;

   int _taggregateVersion = 1;

   public static ISingleTaggregateInstanceTeventStreamMutator Create(ITaggregateTevent creationTevent, IReadOnlyList<ITeventMigration> teventMigrations, Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback = null)
      => new SingleTaggregateInstanceTeventStreamMutator(creationTevent, teventMigrations, teventsAddedCallback);

   SingleTaggregateInstanceTeventStreamMutator
      (ITaggregateTevent creationTevent, IEnumerable<ITeventMigration> teventMigrations, Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback)
   {
      _teventModifier = new TeventModifier(teventsAddedCallback ?? (_ => { }));
      _taggregateId = creationTevent.TaggregateId;
      _teventMigrators = teventMigrations
                       .Where(migration => migration.MigratedTaggregateTeventHierarchyRootInterface.IsInstanceOfType(creationTevent))
                       .Select(migration => migration.CreateSingleTaggregateInstanceHandlingMigrator())
                       .ToArray();
   }

   static IEnumerable<TaggregateTevent> SingleTeventSequence(TaggregateTevent tevent) { yield return tevent; }
   public IEnumerable<TaggregateTevent> Mutate(TaggregateTevent tevent)
   {
      Assert.Argument.Is(_taggregateId == tevent.TaggregateId);
      if (_teventMigrators.Length == 0)
      {
         return SingleTeventSequence(tevent);
      }

#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)tevent).SetTaggregateVersionInternal(_taggregateVersion);
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
      _taggregateVersion += newHistory.Length;
      return newHistory;
   }

   public IEnumerable<TaggregateTevent> EndOfTaggregate()
   {
      return EnumerableCE.Create(new EndOfTaggregateHistoryTeventPlaceHolder(_taggregateId, _taggregateVersion))
                         .SelectMany(Mutate)
                         .Where(tevent => tevent.GetType() != typeof(EndOfTaggregateHistoryTeventPlaceHolder));
   }

   public static TaggregateTevent[] MutateCompleteTaggregateHistory
   (IReadOnlyList<ITeventMigration> teventMigrations,
    TaggregateTevent[] tevents,
    Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback = null)
   {
      if (teventMigrations.None())
      {
         return tevents;
      }

      if(tevents.None())
      {
         return Enumerable.Empty<TaggregateTevent>().ToArray();
      }

      var mutator = Create(tevents.First(), teventMigrations, teventsAddedCallback);

      var result = tevents
                  .SelectMany(mutator.Mutate)
                  .Concat(mutator.EndOfTaggregate())
                  .ToArray();

      AssertMigrationsAreIdempotent(teventMigrations, result);
      TaggregateHistoryValidator.ValidateHistory(result.First().TaggregateId, result);

      return result;
   }

   public static void AssertMigrationsAreIdempotent(IReadOnlyList<ITeventMigration> teventMigrations, TaggregateTevent[] tevents)
   {
      var creationTevent = tevents.First();

      var migrators = teventMigrations
                     .Where(migration => migration.MigratedTaggregateTeventHierarchyRootInterface.IsInstanceOfType(creationTevent))
                     .Select(migration => migration.CreateSingleTaggregateInstanceHandlingMigrator())
                     .ToArray();

      for(var teventIndex = 0; teventIndex < tevents.Length; teventIndex++)
      {
         var tevent = tevents[teventIndex];
         for(var migratorIndex = 0; migratorIndex < migrators.Length; migratorIndex++)
         {
            migrators[migratorIndex].MigrateTevent(tevent, AssertMigrationsAreIdempotentTeventModifier.Instance);
         }
      }
   }
}

public sealed class EndOfTaggregateHistoryTeventPlaceHolder : TaggregateTevent {
#pragma warning disable CS0618 // Type or member is obsolete
    public EndOfTaggregateHistoryTeventPlaceHolder(TaggregateId taggregateId, int i):base(taggregateId) => ((IMutableTaggregateTevent)this).SetTaggregateVersionInternal(i);
#pragma warning restore CS0618 // Type or member is obsolete
}