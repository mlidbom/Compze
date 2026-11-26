using System.Text.Json;
using System.Transactions;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Must;
using Compze.Tessaging.Engine.Wiring;
using Compze.Teventive.MemoryImageStateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.Diagnostics;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.MemoryImageStateStore;

///<summary>A memory image state store keeping vocabs, sentences and decks in the tables of a domain database of the current test's<br/>
/// persistence layer, with an index of the vocabs by question. Each memory image is a container of its own on the database, as each<br/>
/// start of an application is, one after the other.</summary>
public class Given_a_memory_image_state_store_kept_in_the_domain_database : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { Converters = { new TaggregateIdAsAGuid() } };
   const string DomainDatabase = "memory-image-state-store-in-tables";

   ///<summary>The container each memory image is a clone of: clones share its database of the test database pool. It never uses the store itself.</summary>
   readonly IDependencyInjectionContainer _composition = BuildTheComposition();

   IDependencyInjectionContainer? _theMemoryImage;

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   ///<summary>The memory image started last, starting one if none is.</summary>
   IDependencyInjectionContainer TheMemoryImage => _theMemoryImage ??= _composition.CloneAndBuild();

   ///<summary>Disposes the memory image started last, as an application's process does as it ends, and starts the next on the database, as<br/>
   /// the application's next start does.</summary>
   IDependencyInjectionContainer TheNextMemoryImage()
   {
      _theMemoryImage?.Dispose();
      return _theMemoryImage = _composition.CloneAndBuild();
   }

   static IDependencyInjectionContainer BuildTheComposition()
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.CurrentTestsDomainDatabaseWithTheMemoryImageStateStoresSqlLayer(DomainDatabase);
      builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => new StoredVocab())
                                                           .Taggregate(() => new StoredSentence())
                                                           .Taggregate(StoredDeck.Empty)
                                                           .Index<StoredVocab, string>(it => [it.Question])
                                                           .StoredInTheDomainDatabase(Options, rows => rows.ClassNamed<StoredVocab>("vocab")
                                                                                                           .ClassNamed<StoredSentence>("sentence")
                                                                                                           .ClassNamed<StoredDeck>("deck")));
      builder.Registrar.LocalTessagingEngine(_ => {});
      return builder.Build();
   }

   static void InUnitOfWorkOf(IDependencyInjectionContainer memoryImage, Action<ITaggregateStoreUpdater> work) =>
      memoryImage.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   static ITaggregateStoreReader ReaderOf(IDependencyInjectionContainer memoryImage) => memoryImage.RootResolver.Resolve<ITaggregateStoreReader>();

   static IMemoryImageStateStore TheStoreOf(IDependencyInjectionContainer memoryImage) => memoryImage.RootResolver.Resolve<IMemoryImageStateStore>();

   static IReadOnlySet<TaggregateId> IdsOfTheSentencesIn(IDependencyInjectionContainer memoryImage) =>
      memoryImage.RootResolver.Resolve<ITaggregateStoreBulkReader>().ReferencesToAll<StoredSentence>().Select(it => it.Id).ToHashSet();

   static IReadOnlyCollection<TaggregateId> IdsOfTheVocabsWithTheQuestion(IDependencyInjectionContainer memoryImage, string question) =>
      [..memoryImage.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Lookup(question).Select(it => it.Id)];

   protected override async Task DisposeAsyncInternal()
   {
      if(_theMemoryImage != null) await _theMemoryImage.DisposeAsync();
      await _composition.DisposeAsync();
   }

   public class after_a_unit_of_work_adds_a_vocab : Given_a_memory_image_state_store_kept_in_the_domain_database
   {
      public after_a_unit_of_work_adds_a_vocab() => InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));

      [PCT] public void the_next_memory_image_has_the_vocab_with_its_question() =>
         ReaderOf(TheNextMemoryImage()).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");

      [PCT] public void the_next_memory_images_index_finds_the_vocab_by_its_question() =>
         IdsOfTheVocabsWithTheQuestion(TheNextMemoryImage(), "taberu").Must().SequenceEqual([VocabId]);

      public class and_the_memory_image_loads_from_its_tables_again : after_a_unit_of_work_adds_a_vocab
      {
         readonly ChangesFoundInStorage _changes;

         public and_the_memory_image_loads_from_its_tables_again() => _changes = TheStoreOf(TheMemoryImage).LoadFromStorage();

         [PCT] public void it_reports_nothing_added_changed_or_removed() =>
            new { _changes.Added, _changes.Changed, _changes.Removed }.Must().Satisfy(it => it.Added.Count == 0 && it.Changed.Count == 0 && it.Removed.Count == 0);

         [PCT] public void it_still_has_the_vocab() => ReaderOf(TheMemoryImage).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      public class and_a_unit_of_work_changes_its_meaning : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_changes_its_meaning() => InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));

         [PCT] public void the_next_memory_image_has_the_meaning() => ReaderOf(TheNextMemoryImage()).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
      }

      public class and_a_unit_of_work_changes_its_question : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_changes_its_question() => InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeQuestion("nomu"));

         [PCT] public void the_next_memory_images_index_finds_the_vocab_by_its_new_question() => IdsOfTheVocabsWithTheQuestion(TheNextMemoryImage(), "nomu").Must().SequenceEqual([VocabId]);

         [PCT] public void the_next_memory_images_index_finds_nothing_by_its_old_question() => IdsOfTheVocabsWithTheQuestion(TheNextMemoryImage(), "taberu").Must().BeEmpty();
      }

      public class and_a_unit_of_work_deletes_it : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_deletes_it() => InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());

         [PCT] public void the_next_memory_image_has_no_such_vocab() => ReaderOf(TheNextMemoryImage()).TryGet<StoredVocab>(VocabId, out _).Must().BeFalse();

         public class and_a_unit_of_work_adds_a_vocab_with_its_id_again : and_a_unit_of_work_deletes_it
         {
            public and_a_unit_of_work_adds_a_vocab_with_its_id_again() => InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "nomu")));

            [PCT] public void the_next_memory_image_has_the_new_vocab() => ReaderOf(TheNextMemoryImage()).Get<StoredVocab>(VocabId).Question.Must().Be("nomu");
         }
      }

      public class and_the_memory_image_writes_every_taggregate_anew : after_a_unit_of_work_adds_a_vocab
      {
         public and_the_memory_image_writes_every_taggregate_anew() => TheStoreOf(TheMemoryImage).WriteAllToStorage();

         [PCT] public void the_next_memory_image_has_the_vocab() => ReaderOf(TheNextMemoryImage()).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");

         [PCT] public void loading_from_the_tables_again_reports_nothing_changed() => TheStoreOf(TheMemoryImage).LoadFromStorage().Changed.Must().BeEmpty();
      }

      public class and_a_unit_of_work_that_changes_its_meaning_fails_as_its_transaction_prepares : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_that_changes_its_meaning_fails_as_its_transaction_prepares() =>
            Invoking(() => InUnitOfWorkOf(TheMemoryImage, taggregates =>
            {
               taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
               Transaction.Current!.FailOnPrepare();
            })).Must().Throw<TransactionAbortedException>();

         [PCT] public void the_memory_image_still_has_the_vocab_without_a_meaning() => ReaderOf(TheMemoryImage).Get<StoredVocab>(VocabId).Meaning.Must().Be("");

         [PCT] public void the_next_memory_image_has_the_vocab_without_a_meaning() => ReaderOf(TheNextMemoryImage()).Get<StoredVocab>(VocabId).Meaning.Must().Be("");
      }
   }

   public class after_forty_units_of_work_add_cards_to_one_deck_from_many_threads_at_once : Given_a_memory_image_state_store_kept_in_the_domain_database
   {
      const int Cards = 40;
      readonly TaggregateId _deckId = new(Guid.NewGuid());

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.Add(StoredDeck.Create(_deckId)));
         //Each unit of work on a thread of its own, as a test's blocking actors are: forty of them blocked on thread-pool threads
         //starved the pool, and on Linux a synchronous socket read completes only once a pool thread handles the socket's event, so
         //the unit of work holding the deck waited for its database's answer as long as the pool took to grow (2026-11-25).
         var addingTheCards = Enumerable.Range(0, Cards).Select(card => TaskCE.RunOnDedicatedThreadAsync(() => AddACard($"card {card}"))).ToArray();
         //They take a second or two. Far longer means a unit of work holding the deck's lock is stalled: the dump shows on what.
         await Task.WhenAll(addingTheCards).WaitAsyncDumpingTheProcessOnTimeout(TimeSpan.FromSeconds(10), $"the {addingTheCards.Length} units of work adding cards to one deck");
      }

      void AddACard(string text) => InUnitOfWorkOf(TheMemoryImage, taggregates => taggregates.GetForUpdate<StoredDeck>(_deckId).AddCard(text));

      [PCT] public void the_memory_image_has_every_card() => ReaderOf(TheMemoryImage).Get<StoredDeck>(_deckId).Cards.InCreationOrder.Count.Must().Be(Cards);

      [PCT] public void the_next_memory_image_has_every_card() => ReaderOf(TheNextMemoryImage()).Get<StoredDeck>(_deckId).Cards.InCreationOrder.Count.Must().Be(Cards);
   }

   public class after_forty_units_of_work_each_add_a_sentence_from_many_threads_at_once : Given_a_memory_image_state_store_kept_in_the_domain_database
   {
      const int Sentences = 40;
      readonly IReadOnlyList<TaggregateId> _sentenceIds = [..Enumerable.Range(0, Sentences).Select(_ => new TaggregateId(Guid.NewGuid()))];

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         var memoryImage = TheMemoryImage;
         //Rows of taggregates of their own, written at once: each unit of work on a thread of its own, as the deck's are above.
         var addingTheSentences = _sentenceIds.Select(id => TaskCE.RunOnDedicatedThreadAsync(() => InUnitOfWorkOf(memoryImage, taggregates => taggregates.Add(StoredSentence.Create(id, $"sentence {id}"))))).ToArray();
         await Task.WhenAll(addingTheSentences).WaitAsyncDumpingTheProcessOnTimeout(TimeSpan.FromSeconds(10), $"the {addingTheSentences.Length} units of work adding a sentence each");
      }

      [PCT] public void the_next_memory_image_has_every_sentence() =>
         IdsOfTheSentencesIn(TheNextMemoryImage()).Must().Satisfy(it => it.SetEquals(_sentenceIds));
   }
}
