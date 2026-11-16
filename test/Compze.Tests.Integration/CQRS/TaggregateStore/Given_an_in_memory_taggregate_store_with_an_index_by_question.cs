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
using Compze.Teventive.TaggregateStore;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TaggregateStore;

///<summary>A prototype: taggregate-references, and an index defined by the taggregates' state that reads its own unit of<br/>
/// work's changes, in an in-memory taggregate store holding one vocab, "taberu".</summary>
public class Given_an_in_memory_taggregate_store_with_an_index_by_question : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   protected IDependencyInjectionContainer Container => _container;
   protected TaggregateId VocabId { get; } = new(Guid.NewGuid());

   protected Given_an_in_memory_taggregate_store_with_an_index_by_question()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.InMemoryTaggregateStore(store => store.Index<StoredVocab, string>(vocab => [vocab.Question]));
      builder.Registrar.LocalTessagingEngine(_ => {});
      _container = builder.Build();
      InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
   }

   protected void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   protected ITaggregateStoreReader Reader => Container.RootResolver.Resolve<ITaggregateStoreReader>();

   private protected ITaggregateIndex<StoredVocab, string> ByQuestion => Container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>();

   private protected ITaggregateReference<StoredVocab> ReferenceTo(TaggregateId id) => Reader.Reference<StoredVocab>(id);

   protected IReadOnlyList<TaggregateId> IdsFiledUnder(string question) => [..ByQuestion.Lookup(question).Select(it => it.Id)];

   ///<summary>Runs <paramref name="read"/> on a thread of its own, with no ambient transaction flowing to it. Not a task: waiting<br/>
   /// for a task that hasn't started may run it inline, on the waiting thread, inside the transaction.</summary>
   protected static TResult OnAThreadOutsideTheUnitOfWork<TResult>(Func<TResult> read)
   {
      var result = default(TResult)!;
      var thread = new Thread(() => result = read());
      using(ExecutionContext.SuppressFlow()) thread.Start();
      thread.Join();
      return result;
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class while_a_unit_of_work_renames_the_vocab_and_after_it_commits : Given_an_in_memory_taggregate_store_with_an_index_by_question
   {
      readonly ITaggregateReference<StoredVocab> _referenceHeldThroughout;
      readonly IReadOnlyList<TaggregateId> _filedUnderTheOldQuestionInside = [];
      readonly IReadOnlyList<TaggregateId> _filedUnderTheNewQuestionInside = [];
      readonly IReadOnlyList<TaggregateId> _filedUnderTheOldQuestionOutside = [];
      readonly string _questionTheReferenceGaveInside = "";
      readonly string _questionTheReferenceGaveOutside = "";

      public while_a_unit_of_work_renames_the_vocab_and_after_it_commits()
      {
         _referenceHeldThroughout = ReferenceTo(VocabId);
         IReadOnlyList<TaggregateId> oldInside = [], newInside = [], oldOutside = [];
         string referenceInside = "", referenceOutside = "";
         InUnitOfWork(_ =>
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

   public class after_a_unit_of_work_that_renamed_the_vocab_throws : Given_an_in_memory_taggregate_store_with_an_index_by_question
   {
      public after_a_unit_of_work_that_renamed_the_vocab_throws() =>
         Invoking(() => InUnitOfWork(_ =>
                 {
                    ReferenceTo(VocabId).ForUpdate().ChangeQuestion("kuu");
                    throw new InvalidOperationException("The unit of work fails.");
                 }))
           .Must().Throw<InvalidOperationException>();

      [PCT] public void a_lookup_of_the_old_question_finds_the_vocab() => IdsFiledUnder("taberu").Must().SequenceEqual([VocabId]);
      [PCT] public void a_lookup_of_the_new_question_finds_nothing() => IdsFiledUnder("kuu").Must().BeEmpty();
      [PCT] public void the_reference_gives_the_old_question() => ReferenceTo(VocabId).Current.Question.Must().Be("taberu");
   }

   public class after_a_unit_of_work_twice_creates_a_vocab_for_a_question_unless_one_is_filed_under_it : Given_an_in_memory_taggregate_store_with_an_index_by_question
   {
      public after_a_unit_of_work_twice_creates_a_vocab_for_a_question_unless_one_is_filed_under_it() =>
         InUnitOfWork(taggregates =>
         {
            CreateUnlessOneIsFiledUnder("kuu");
            CreateUnlessOneIsFiledUnder("kuu");

            void CreateUnlessOneIsFiledUnder(string question)
            {
               if(IdsFiledUnder(question).Count == 0) taggregates.Save(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), question));
            }
         });

      [PCT] public void one_vocab_is_filed_under_the_question() => IdsFiledUnder("kuu").Must().HaveCount(1);
   }

   public class after_a_unit_of_work_deletes_the_vocab : Given_an_in_memory_taggregate_store_with_an_index_by_question
   {
      readonly IReadOnlyList<TaggregateId> _filedUnderItsQuestionInside = [];

      public after_a_unit_of_work_deletes_the_vocab()
      {
         IReadOnlyList<TaggregateId> inside = [];
         InUnitOfWork(taggregates =>
         {
            taggregates.Delete(VocabId);
            inside = IdsFiledUnder("taberu");
         });
         _filedUnderItsQuestionInside = inside;
      }

      [PCT] public void inside_the_unit_of_work_a_lookup_of_its_question_found_nothing() => _filedUnderItsQuestionInside.Must().BeEmpty();
      [PCT] public void a_lookup_of_its_question_finds_nothing() => IdsFiledUnder("taberu").Must().BeEmpty();
      [PCT] public void its_reference_throws() => Invoking(() => ReferenceTo(VocabId).Current).Must().Throw<Exception>();
   }

   public class outside_any_unit_of_work : Given_an_in_memory_taggregate_store_with_an_index_by_question
   {
      [PCT] public void getting_the_vocab_for_update_through_its_reference_throws() => Invoking(() => ReferenceTo(VocabId).ForUpdate()).Must().Throw<InvalidOperationException>();
   }

   public class when_measuring : Given_an_in_memory_taggregate_store_with_an_index_by_question
   {
      [PCT] public void what_a_lookup_costs()
      {
         var reportFile = Environment.GetEnvironmentVariable("TAGGREGATE_STORE_MEASUREMENT_FILE");
         if(reportFile == null) return;

         var reference = ReferenceTo(VocabId);
         var report = new List<string>
         {
            Measure("a lookup outside any unit of work", 20_000, () => ByQuestion.Lookup("taberu")),
            Measure("a reference's Current outside any unit of work", 20_000, () => _ = reference.Current)
         };

         foreach(var copiesHeld in (int[])[1, 100, 1_000])
         {
            var ids = Enumerable.Range(0, copiesHeld).Select(_ => new TaggregateId(Guid.NewGuid())).ToList();
            InUnitOfWork(taggregates => ids.ForEach(id => taggregates.Save(StoredVocab.Create(id, id.ToString()))));
            InUnitOfWork(taggregates =>
            {
               ids.ForEach(id => taggregates.Get<StoredVocab>(id));
               report.Add(Measure($"a lookup inside a unit of work holding {copiesHeld.ToString(CultureInfo.InvariantCulture)} copies", 2_000, () => ByQuestion.Lookup("taberu")));
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
