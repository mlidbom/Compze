using System.Diagnostics;
using System.Globalization;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.StateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>Taggregate-references, and an index defined by the taggregates' state that reads its own unit of work's changes, in a<br/>
/// state store holding one vocab, "taberu". The units of work here resolve nothing: references find their unit of work from the<br/>
/// ambient transaction alone.</summary>
public class Given_a_state_store_with_an_index_by_question : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   IDependencyInjectionContainer Container => _container;
   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   protected Given_a_state_store_with_an_index_by_question()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.StateStore(store => store.Taggregate(() => new StoredVocab())
                                                 .Taggregate(() => new StoredSentence())
                                                 .Index<StoredVocab, string>(vocab => [vocab.Question]));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _container = builder.Build();
      InUnitOfWorkWithItsUpdater(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
   }

   ///<summary>Runs <paramref name="work"/> as a unit of work that resolves nothing from its scope.</summary>
   void InUnitOfWork(Action work) => Container.ScopeFactory.ExecuteUnitOfWork(_ => work());

   void InUnitOfWorkWithItsUpdater(Action<ITaggregateStoreUpdater> work) =>
      Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => Container.RootResolver.Resolve<ITaggregateStoreReader>();

   ITaggregateIndex<StoredVocab, string> ByQuestion => Container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>();

   ITaggregateReference<StoredVocab> ReferenceTo(TaggregateId id) => Reader.Reference<StoredVocab>(id);

   IReadOnlyList<TaggregateId> IdsFiledUnder(string question) => [..ByQuestion.Lookup(question).Select(it => it.Id)];

   ///<summary>Runs <paramref name="read"/> on a thread of its own, with no ambient transaction flowing to it. Not a task: waiting<br/>
   /// for a task that hasn't started may run it inline, on the waiting thread, inside the transaction.</summary>
   static TResult OnAThreadOutsideTheUnitOfWork<TResult>(Func<TResult> read)
   {
      var result = default(TResult)!;
      var thread = new Thread(() => result = read());
      using(ExecutionContext.SuppressFlow()) thread.Start();
      thread.Join();
      return result;
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class after_three_units_of_work_each_save_a_vocab_with_the_same_question : Given_a_state_store_with_an_index_by_question
   {
      readonly TaggregateId[] _firstThree = [new(), new(), new()];
      readonly TaggregateId _fourth = new();

      public after_three_units_of_work_each_save_a_vocab_with_the_same_question()
      {
         foreach(var id in _firstThree)
            InUnitOfWorkWithItsUpdater(taggregates => taggregates.Save(StoredVocab.Create(id, "kuu")));
      }

      [PCT] public void the_lookup_gives_the_three() => IdsFiledUnder("kuu").Must().Satisfy(it => it.Count == 3 && _firstThree.All(it.Contains));

      public class and_a_unit_of_work_saves_a_fourth : after_three_units_of_work_each_save_a_vocab_with_the_same_question
      {
         IReadOnlyList<TaggregateId> _filedInsideTheUnitOfWork = [];

         public and_a_unit_of_work_saves_a_fourth() =>
            InUnitOfWorkWithItsUpdater(taggregates =>
            {
               taggregates.Save(StoredVocab.Create(_fourth, "kuu"));
               _filedInsideTheUnitOfWork = IdsFiledUnder("kuu");
            });

         [PCT] public void inside_the_unit_of_work_the_lookup_gives_all_four() => _filedInsideTheUnitOfWork.Must().Satisfy(it => it.Count == 4 && _firstThree.Append(_fourth).All(it.Contains));
         [PCT] public void after_it_commits_the_lookup_gives_all_four() => IdsFiledUnder("kuu").Must().Satisfy(it => it.Count == 4 && _firstThree.Append(_fourth).All(it.Contains));
      }
   }

   public class while_a_unit_of_work_renames_the_vocab_through_a_reference_and_after_it_commits : Given_a_state_store_with_an_index_by_question
   {
      readonly ITaggregateReference<StoredVocab> _referenceHeldThroughout;
      readonly IReadOnlyList<TaggregateId> _filedUnderTheOldQuestionInside;
      readonly IReadOnlyList<TaggregateId> _filedUnderTheNewQuestionInside;
      readonly IReadOnlyList<TaggregateId> _filedUnderTheOldQuestionOutside;
      readonly string _questionTheReferenceGaveInside;
      readonly string _questionTheReferenceGaveOutside;

      public while_a_unit_of_work_renames_the_vocab_through_a_reference_and_after_it_commits()
      {
         _referenceHeldThroughout = ReferenceTo(VocabId);
         IReadOnlyList<TaggregateId> oldInside = [], newInside = [], oldOutside = [];
         string referenceInside = "", referenceOutside = "";
         InUnitOfWork(() =>
         {
            _referenceHeldThroughout.ForUpdate().ChangeQuestion("kuu");
            oldInside = IdsFiledUnder("taberu");
            newInside = IdsFiledUnder("kuu");
            oldOutside = OnAThreadOutsideTheUnitOfWork(() => IdsFiledUnder("taberu"));
            referenceInside = _referenceHeldThroughout.Current.Question;
            referenceOutside = OnAThreadOutsideTheUnitOfWork(() => _referenceHeldThroughout.Current.Question);
         });
         (_filedUnderTheOldQuestionInside, _filedUnderTheNewQuestionInside, _filedUnderTheOldQuestionOutside) = (oldInside, newInside, oldOutside);
         (_questionTheReferenceGaveInside, _questionTheReferenceGaveOutside) = (referenceInside, referenceOutside);
      }

      [PCT] public void inside_the_unit_of_work_a_lookup_of_the_old_question_finds_nothing() => _filedUnderTheOldQuestionInside.Must().BeEmpty();
      [PCT] public void inside_the_unit_of_work_a_lookup_of_the_new_question_finds_the_vocab() => _filedUnderTheNewQuestionInside.Must().SequenceEqual([VocabId]);
      [PCT] public void outside_the_unit_of_work_a_lookup_of_the_old_question_still_finds_the_vocab() => _filedUnderTheOldQuestionOutside.Must().SequenceEqual([VocabId]);
      [PCT] public void inside_the_unit_of_work_the_reference_gives_its_copy() => _questionTheReferenceGaveInside.Must().Be("kuu");
      [PCT] public void outside_the_unit_of_work_the_reference_gives_the_committed_instance() => _questionTheReferenceGaveOutside.Must().Be("taberu");
      [PCT] public void after_the_commit_a_lookup_of_the_new_question_finds_the_vocab() => IdsFiledUnder("kuu").Must().SequenceEqual([VocabId]);
      [PCT] public void after_the_commit_a_lookup_of_the_old_question_finds_nothing() => IdsFiledUnder("taberu").Must().BeEmpty();
      [PCT] public void after_the_commit_the_reference_held_throughout_gives_the_new_committed_instance() => _referenceHeldThroughout.Current.Question.Must().Be("kuu");
   }

   public class after_a_unit_of_work_that_renamed_the_vocab_throws : Given_a_state_store_with_an_index_by_question
   {
      public after_a_unit_of_work_that_renamed_the_vocab_throws() =>
         Invoking(() => InUnitOfWork(() =>
                 {
                    ReferenceTo(VocabId).ForUpdate().ChangeQuestion("kuu");
                    throw new InvalidOperationException("The unit of work fails.");
                 }))
           .Must().Throw<InvalidOperationException>();

      [PCT] public void a_lookup_of_the_old_question_finds_the_vocab() => IdsFiledUnder("taberu").Must().SequenceEqual([VocabId]);
      [PCT] public void a_lookup_of_the_new_question_finds_nothing() => IdsFiledUnder("kuu").Must().BeEmpty();
      [PCT] public void the_reference_gives_the_old_question() => ReferenceTo(VocabId).Current.Question.Must().Be("taberu");
   }

   public class after_a_unit_of_work_twice_creates_a_vocab_for_a_question_unless_one_is_filed_under_it : Given_a_state_store_with_an_index_by_question
   {
      public after_a_unit_of_work_twice_creates_a_vocab_for_a_question_unless_one_is_filed_under_it() =>
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
         {
            var taggregates = unitOfWork.Resolve<ITaggregateStoreUpdater>();
            CreateUnlessOneIsFiledUnder("kuu");
            CreateUnlessOneIsFiledUnder("kuu");

            void CreateUnlessOneIsFiledUnder(string question)
            {
               if(!ByQuestion.HasAny(question)) taggregates.Save(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), question));
            }
         });

      [PCT] public void one_vocab_is_filed_under_the_question() => IdsFiledUnder("kuu").Must().HaveCount(1);
   }

   public class after_a_unit_of_work_deletes_the_vocab : Given_a_state_store_with_an_index_by_question
   {
      readonly IReadOnlyList<TaggregateId> _filedUnderItsQuestionInside;

      public after_a_unit_of_work_deletes_the_vocab()
      {
         IReadOnlyList<TaggregateId> inside = [];
         Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
         {
            unitOfWork.Resolve<ITaggregateStoreUpdater>().GetForUpdate<StoredVocab>(VocabId).Delete();
            inside = IdsFiledUnder("taberu");
         });
         _filedUnderItsQuestionInside = inside;
      }

      [PCT] public void inside_the_unit_of_work_a_lookup_of_its_question_found_nothing() => _filedUnderItsQuestionInside.Must().BeEmpty();
      [PCT] public void a_lookup_of_its_question_finds_nothing() => IdsFiledUnder("taberu").Must().BeEmpty();
      [PCT] public void its_reference_throws() => Invoking(() => ReferenceTo(VocabId).Current).Must().Throw<Exception>();
   }

   public class outside_any_unit_of_work : Given_a_state_store_with_an_index_by_question
   {
      [PCT] public void getting_the_vocab_for_update_through_its_reference_throws() => Invoking(() => ReferenceTo(VocabId).ForUpdate()).Must().Throw<Exception>();
   }

   public class when_measuring : Given_a_state_store_with_an_index_by_question
   {
      [PCT] public void what_a_lookup_costs()
      {
         var reportFile = Environment.GetEnvironmentVariable("TAGGREGATE_STORE_MEASUREMENT_FILE");
         if(reportFile == null) return;

         var reference = ReferenceTo(VocabId);
         var byQuestion = ByQuestion; //Resolved once: resolving from the container allocates, and is no part of a lookup.
         var report = new List<string>
         {
            Measure("a lookup outside any unit of work", 20_000, () => byQuestion.Lookup("taberu")),
            Measure("a check that a key has any taggregate, outside any unit of work", 20_000, () => byQuestion.HasAny("taberu")),
            Measure("a reference's Current outside any unit of work", 20_000, () => _ = reference.Current)
         };

         var sentenceId = new TaggregateId(Guid.NewGuid());
         InUnitOfWorkWithItsUpdater(taggregates => taggregates.Save(StoredSentence.Create(sentenceId, "taberu")));
         InUnitOfWorkWithItsUpdater(taggregates =>
         {
            taggregates.GetForUpdate<StoredSentence>(sentenceId);
            report.Add(Measure("a lookup of vocab inside a unit of work holding only a sentence", 20_000, () => byQuestion.Lookup("taberu")));
            report.Add(Measure("a check that a key has any vocab inside a unit of work holding only a sentence", 20_000, () => byQuestion.HasAny("taberu")));
         });

         foreach(var copiesHeld in (int[])[1, 100, 1_000])
         {
            var ids = Enumerable.Range(0, copiesHeld).Select(_ => new TaggregateId(Guid.NewGuid())).ToList();
            InUnitOfWorkWithItsUpdater(taggregates => ids.ForEach(id => taggregates.Save(StoredVocab.Create(id, id.ToString()))));
            InUnitOfWorkWithItsUpdater(taggregates =>
            {
               ids.ForEach(id => taggregates.GetForUpdate<StoredVocab>(id));
               report.Add(Measure($"a lookup inside a unit of work holding {copiesHeld.ToString(CultureInfo.InvariantCulture)} copies", 2_000, () => byQuestion.Lookup("taberu")));
            });
         }

         File.AppendAllLines(reportFile, report);
      }

      static string Measure(string what, int iterations, Action operation)
      {
         for(var iteration = 0; iteration < iterations / 10; iteration++) operation();
         var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
         var stopwatch = Stopwatch.StartNew();
         for(var iteration = 0; iteration < iterations; iteration++) operation();
         stopwatch.Stop();
         var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
         return $"{what}: {stopwatch.Elapsed.TotalMicroseconds / iterations:F3} µs and {allocated / iterations:N0} B allocated, each, over {iterations:N0}";
      }
   }
}
