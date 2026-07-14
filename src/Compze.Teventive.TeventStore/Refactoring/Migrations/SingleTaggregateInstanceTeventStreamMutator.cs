using Compze.Abstractions.Public;
using Compze.Contracts;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Internal;
using Compze.Teventive.TeventStore.Abstractions.Refactoring.Migrations.Public;

// ReSharper disable ForCanBeConvertedToForeach

namespace Compze.Teventive.TeventStore.Refactoring.Migrations;

//Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot.
//What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
//The performance of this class is extremely important since it is called at least once for every tevent that is loaded from the tevent store when you have any migrations activated. It is called A LOT.
//This is one of those central classes for which optimization is actually vitally important.
//Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.
class SingleTaggregateInstanceTeventStreamMutator : ISingleTaggregateInstanceTeventStreamMutator
{
   readonly TaggregateId _taggregateId;
   readonly ISingleTaggregateInstanceHandlingTeventMigrator[] _teventMigrators;
   readonly TeventModifier _teventModifier;

   int _taggregateVersion = 1;

   public static ISingleTaggregateInstanceTeventStreamMutator Create(ITaggregateTevent<ITaggregateTevent> creationTevent, IReadOnlyList<ITeventMigration> teventMigrations, Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback = null)
      => new SingleTaggregateInstanceTeventStreamMutator(creationTevent, teventMigrations, teventsAddedCallback);

   SingleTaggregateInstanceTeventStreamMutator
      (ITaggregateTevent<ITaggregateTevent> creationTevent, IEnumerable<ITeventMigration> teventMigrations, Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback)
   {
      _teventModifier = new TeventModifier(teventsAddedCallback ?? (_ => { }));
      _taggregateId = creationTevent.Tevent.TaggregateId;
      _teventMigrators = teventMigrations
                       .Where(migration => migration.MigratedTaggregateTeventHierarchyRootInterface.IsInstanceOfType(creationTevent.Tevent))
                       .Select(migration => migration.CreateSingleTaggregateInstanceHandlingMigrator())
                       .ToArray();
   }

   static IEnumerable<ITaggregateTevent<ITaggregateTevent>> SingleTeventSequence(ITaggregateTevent<ITaggregateTevent> wrappedTevent) { yield return wrappedTevent; }
   public IEnumerable<ITaggregateTevent<ITaggregateTevent>> Mutate(ITaggregateTevent<ITaggregateTevent> wrappedTevent)
   {
      Contract.Argument.Assert(_taggregateId == wrappedTevent.Tevent.TaggregateId);
      if (_teventMigrators.Length == 0)
      {
         return SingleTeventSequence(wrappedTevent);
      }

#pragma warning disable CS0618 // Type or member is obsolete
      ((IMutableTaggregateTevent)wrappedTevent.Tevent).SetTaggregateVersionInternal(_taggregateVersion);
#pragma warning restore CS0618 // Type or member is obsolete
      _teventModifier.Reset(wrappedTevent);

      for(var index = 0; index < _teventMigrators.Length; index++)
      {
         if (_teventModifier.WrappedTevents == null)
         {
            _teventMigrators[index].MigrateTevent(wrappedTevent, _teventModifier);
         }
         else
         {
            var node = _teventModifier.WrappedTevents.First;
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

   public IEnumerable<ITaggregateTevent<ITaggregateTevent>> EndOfTaggregate()
   {
      ITaggregateTevent<ITaggregateTevent> endOfHistoryPlaceHolder = new TaggregateTevent<EndOfTaggregateHistoryTeventPlaceHolder>(new EndOfTaggregateHistoryTeventPlaceHolder(_taggregateId, _taggregateVersion));
      return EnumerableCE.Create(endOfHistoryPlaceHolder)
                         .SelectMany(Mutate)
                         .Where(wrappedTevent => wrappedTevent.Tevent.GetType() != typeof(EndOfTaggregateHistoryTeventPlaceHolder));
   }

   public static ITaggregateTevent<ITaggregateTevent>[] MutateCompleteTaggregateHistory
   (IReadOnlyList<ITeventMigration> teventMigrations,
    ITaggregateTevent<ITaggregateTevent>[] wrappedTevents,
    Action<IReadOnlyList<TeventModifier.RefactoredTevent>>? teventsAddedCallback = null)
   {
      if (teventMigrations.None())
      {
         return wrappedTevents;
      }

      if(wrappedTevents.None())
      {
         return Enumerable.Empty<ITaggregateTevent<ITaggregateTevent>>().ToArray();
      }

      var mutator = Create(wrappedTevents.First(), teventMigrations, teventsAddedCallback);

      var result = wrappedTevents
                  .SelectMany(mutator.Mutate)
                  .Concat(mutator.EndOfTaggregate())
                  .ToArray();

      AssertMigrationsAreIdempotent(teventMigrations, result);
      TaggregateHistoryValidator.ValidateHistory(result.First().Tevent.TaggregateId, result);

      return result;
   }

   public static void AssertMigrationsAreIdempotent(IReadOnlyList<ITeventMigration> teventMigrations, ITaggregateTevent<ITaggregateTevent>[] wrappedTevents)
   {
      var creationTevent = wrappedTevents.First();

      var migrators = teventMigrations
                     .Where(migration => migration.MigratedTaggregateTeventHierarchyRootInterface.IsInstanceOfType(creationTevent.Tevent))
                     .Select(migration => migration.CreateSingleTaggregateInstanceHandlingMigrator())
                     .ToArray();

      for(var teventIndex = 0; teventIndex < wrappedTevents.Length; teventIndex++)
      {
         var wrappedTevent = wrappedTevents[teventIndex];
         for(var migratorIndex = 0; migratorIndex < migrators.Length; migratorIndex++)
         {
            migrators[migratorIndex].MigrateTevent(wrappedTevent, AssertMigrationsAreIdempotentTeventModifier.Instance);
         }
      }
   }
}

sealed class EndOfTaggregateHistoryTeventPlaceHolder : TaggregateTevent {
#pragma warning disable CS0618 // Type or member is obsolete
    public EndOfTaggregateHistoryTeventPlaceHolder(TaggregateId taggregateId, int i):base(taggregateId) => ((IMutableTaggregateTevent)this).SetTaggregateVersionInternal(i);
#pragma warning restore CS0618 // Type or member is obsolete
}
