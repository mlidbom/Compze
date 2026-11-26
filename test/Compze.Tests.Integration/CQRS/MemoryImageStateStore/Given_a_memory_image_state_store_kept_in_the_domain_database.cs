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
/// persistence layer, with an index of the vocabs by question. Each memory image is a container of its own on the same database, as<br/>
/// each process, or each endpoint of a process, that shares the database is. The snapshots are written with <see cref="JsonSerializerOptions"/><br/>
/// that have no converter of their own, as the <see cref="TaggregateId"/>s they hold need none.</summary>
public class Given_a_memory_image_state_store_kept_in_the_domain_database : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new();
   const string DomainDatabase = "memory-image-state-store-in-tables";
   readonly List<IDependencyInjectionContainer> _memoryImages = [];

   TaggregateId VocabId { get; } = new(Guid.NewGuid());

   ///<summary>The memory image started first, starting it if none is.</summary>
   IDependencyInjectionContainer TheFirst => _memoryImages.Count > 0 ? _memoryImages[0] : StartAMemoryImage();

   ///<summary>The memory image started second, starting it if it isn't, and the first before it.</summary>
   IDependencyInjectionContainer TheSecond
   {
      get
      {
         _ = TheFirst;
         return _memoryImages.Count > 1 ? _memoryImages[1] : StartAMemoryImage();
      }
   }

   ///<summary>Starts a memory image on the database, a container of its own: the first builds the container, which takes a database<br/>
   /// of the test database pool; each later one is a clone of it, which shares its database.</summary>
   IDependencyInjectionContainer StartAMemoryImage()
   {
      var memoryImage = _memoryImages.Count == 0 ? BuildTheFirstMemoryImage() : _memoryImages[0].CloneAndBuild();
      _memoryImages.Add(memoryImage);
      return memoryImage;
   }

   static IDependencyInjectionContainer BuildTheFirstMemoryImage()
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

   static IReadOnlyCollection<TaggregateId> IdsOfTheVocabsWithTheQuestion(IDependencyInjectionContainer memoryImage, string question) =>
      [..memoryImage.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Lookup(question).Select(it => it.Id)];

   ///<summary>A unit of work of <paramref name="memoryImage"/> that adds a sentence: the memory image catches up at its first change of the store.</summary>
   static void HaveAUnitOfWorkOfItChangeTheStore(IDependencyInjectionContainer memoryImage) =>
      InUnitOfWorkOf(memoryImage, taggregates => taggregates.Add(StoredSentence.Create(new TaggregateId(Guid.NewGuid()), "a sentence")));

   protected override async Task DisposeAsyncInternal()
   {
      foreach(var memoryImage in Enumerable.Reverse(_memoryImages)) await memoryImage.DisposeAsync();
   }

   public class after_a_unit_of_work_adds_a_vocab : Given_a_memory_image_state_store_kept_in_the_domain_database
   {
      public after_a_unit_of_work_adds_a_vocab() => InUnitOfWorkOf(TheFirst, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));

      [PCT] public void another_memory_image_started_on_the_database_has_the_vocab_with_its_question() =>
         ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");

      [PCT] public void another_memory_image_started_on_the_database_has_the_vocab_with_the_id_its_snapshot_holds() =>
         ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Id.Must().Be(VocabId);

      public class and_the_memory_image_loads_from_its_tables_again : after_a_unit_of_work_adds_a_vocab
      {
         readonly ChangesFoundInStorage _changes;

         public and_the_memory_image_loads_from_its_tables_again() => _changes = TheStoreOf(TheFirst).LoadFromStorage();

         [PCT] public void it_reports_nothing_added_changed_or_removed() =>
            new { _changes.Added, _changes.Changed, _changes.Removed }.Must().Satisfy(it => it.Added.Count == 0 && it.Changed.Count == 0 && it.Removed.Count == 0);

         [PCT] public void it_still_has_the_vocab() => ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      public class and_a_unit_of_work_changes_its_meaning : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_changes_its_meaning() => InUnitOfWorkOf(TheFirst, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));

         [PCT] public void another_memory_image_started_on_the_database_has_the_meaning() =>
            ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
      }

      public class and_a_unit_of_work_deletes_it : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_deletes_it() => InUnitOfWorkOf(TheFirst, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());

         [PCT] public void another_memory_image_started_on_the_database_has_no_such_vocab() =>
            ReaderOf(TheSecond).TryGet<StoredVocab>(VocabId, out _).Must().BeFalse();

         public class and_a_unit_of_work_adds_a_vocab_with_its_id_again : and_a_unit_of_work_deletes_it
         {
            public and_a_unit_of_work_adds_a_vocab_with_its_id_again() => InUnitOfWorkOf(TheFirst, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "nomu")));

            [PCT] public void another_memory_image_started_on_the_database_has_the_new_vocab() =>
               ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question.Must().Be("nomu");
         }
      }

      public class and_the_memory_image_writes_every_taggregate_anew : after_a_unit_of_work_adds_a_vocab
      {
         public and_the_memory_image_writes_every_taggregate_anew() => TheStoreOf(TheFirst).WriteAllToStorage();

         [PCT] public void another_memory_image_started_on_the_database_has_the_vocab() =>
            ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");

         [PCT] public void loading_from_the_tables_again_reports_nothing_changed() =>
            TheStoreOf(TheFirst).LoadFromStorage().Changed.Must().BeEmpty();
      }

      public class and_a_unit_of_work_that_changes_its_meaning_fails_as_its_transaction_prepares : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_unit_of_work_that_changes_its_meaning_fails_as_its_transaction_prepares() =>
            Invoking(() => InUnitOfWorkOf(TheFirst, taggregates =>
            {
               taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat");
               Transaction.Current!.FailOnPrepare();
            })).Must().Throw<TransactionAbortedException>();

         [PCT] public void the_memory_image_still_has_the_vocab_without_a_meaning() => ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("");

         [PCT] public void another_memory_image_started_on_the_database_has_the_vocab_without_a_meaning() => ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Meaning.Must().Be("");
      }

      public class and_a_second_memory_image_has_read_it : after_a_unit_of_work_adds_a_vocab
      {
         public and_a_second_memory_image_has_read_it() => ReaderOf(TheSecond).Get<StoredVocab>(VocabId);

         public class and_a_unit_of_work_of_the_second_changes_its_meaning : and_a_second_memory_image_has_read_it
         {
            public and_a_unit_of_work_of_the_second_changes_its_meaning() => InUnitOfWorkOf(TheSecond, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));

            [PCT] public void outside_any_unit_of_work_the_first_reads_the_meaning_it_last_caught_up_with() => ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("");

            public class and_a_unit_of_work_of_the_first_gets_it_for_update : and_a_unit_of_work_of_the_second_changes_its_meaning
            {
               string? _meaningItGot;

               public and_a_unit_of_work_of_the_first_gets_it_for_update() => InUnitOfWorkOf(TheFirst, taggregates => _meaningItGot = taggregates.GetForUpdate<StoredVocab>(VocabId).Meaning);

               [PCT] public void it_gets_the_vocab_with_the_meaning_the_second_committed() => _meaningItGot.Must().Be("to eat");

               [PCT] public void the_first_reads_the_meaning_the_second_committed() => ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
            }

            public class and_a_unit_of_work_of_the_first_adds_a_sentence : and_a_unit_of_work_of_the_second_changes_its_meaning
            {
               public and_a_unit_of_work_of_the_first_adds_a_sentence() => HaveAUnitOfWorkOfItChangeTheStore(TheFirst);

               [PCT] public void the_first_has_caught_up_and_reads_the_meaning_the_second_committed() => ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat");
            }

            public class and_a_unit_of_work_of_the_first_adds_to_the_meaning : and_a_unit_of_work_of_the_second_changes_its_meaning
            {
               public and_a_unit_of_work_of_the_first_adds_to_the_meaning() =>
                  InUnitOfWorkOf(TheFirst, taggregates =>
                  {
                     var vocab = taggregates.GetForUpdate<StoredVocab>(VocabId);
                     vocab.ChangeMeaning($"{vocab.Meaning}, and to drink");
                  });

               [PCT] public void the_first_added_to_what_the_second_committed_so_no_change_is_lost() =>
                  ReaderOf(TheFirst).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat, and to drink");

               public class and_a_unit_of_work_of_the_second_adds_a_sentence : and_a_unit_of_work_of_the_first_adds_to_the_meaning
               {
                  public and_a_unit_of_work_of_the_second_adds_a_sentence() => HaveAUnitOfWorkOfItChangeTheStore(TheSecond);

                  [PCT] public void the_second_has_caught_up_and_reads_the_meaning_the_first_committed() =>
                     ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Meaning.Must().Be("to eat, and to drink");
               }
            }
         }

         public class and_a_unit_of_work_of_the_second_changes_its_question_and_one_of_the_first_adds_a_sentence : and_a_second_memory_image_has_read_it
         {
            public and_a_unit_of_work_of_the_second_changes_its_question_and_one_of_the_first_adds_a_sentence()
            {
               InUnitOfWorkOf(TheSecond, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeQuestion("nomu"));
               HaveAUnitOfWorkOfItChangeTheStore(TheFirst);
            }

            [PCT] public void the_firsts_index_finds_the_vocab_by_its_new_question() => IdsOfTheVocabsWithTheQuestion(TheFirst, "nomu").Must().SequenceEqual([VocabId]);

            [PCT] public void the_firsts_index_finds_nothing_by_its_old_question() => IdsOfTheVocabsWithTheQuestion(TheFirst, "taberu").Must().BeEmpty();
         }

         public class and_a_unit_of_work_of_the_second_deletes_it_and_one_of_the_first_adds_a_sentence : and_a_second_memory_image_has_read_it
         {
            public and_a_unit_of_work_of_the_second_deletes_it_and_one_of_the_first_adds_a_sentence()
            {
               InUnitOfWorkOf(TheSecond, taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).Delete());
               HaveAUnitOfWorkOfItChangeTheStore(TheFirst);
            }

            [PCT] public void the_first_has_no_such_vocab() => ReaderOf(TheFirst).TryGet<StoredVocab>(VocabId, out _).Must().BeFalse();

            [PCT] public void the_firsts_index_finds_nothing_by_its_question() => IdsOfTheVocabsWithTheQuestion(TheFirst, "taberu").Must().BeEmpty();
         }

         public class and_a_unit_of_work_of_the_second_adds_a_vocab_with_the_same_id : and_a_second_memory_image_has_read_it
         {
            readonly Exception _thrown;

            public and_a_unit_of_work_of_the_second_adds_a_vocab_with_the_same_id() =>
               _thrown = Invoking(() => InUnitOfWorkOf(TheSecond, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "nomu")))).Must().Throw<Exception>().Which;

            [PCT] public void it_fails_saying_the_store_already_holds_a_taggregate_with_the_id() => _thrown.Message.Must().Contain($"already holds a taggregate {VocabId}");
         }
      }
   }

   public class after_a_unit_of_work_of_the_first_memory_image_adds_a_vocab_that_a_started_second_has_not_seen : Given_a_memory_image_state_store_kept_in_the_domain_database
   {
      public after_a_unit_of_work_of_the_first_memory_image_adds_a_vocab_that_a_started_second_has_not_seen()
      {
         ReaderOf(TheSecond).TryGet<StoredVocab>(VocabId, out _);
         InUnitOfWorkOf(TheFirst, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "taberu")));
      }

      [PCT] public void outside_any_unit_of_work_the_second_reads_no_such_vocab() => ReaderOf(TheSecond).TryGet<StoredVocab>(VocabId, out _).Must().BeFalse();

      public class and_a_unit_of_work_of_the_second_adds_a_sentence : after_a_unit_of_work_of_the_first_memory_image_adds_a_vocab_that_a_started_second_has_not_seen
      {
         public and_a_unit_of_work_of_the_second_adds_a_sentence() => HaveAUnitOfWorkOfItChangeTheStore(TheSecond);

         [PCT] public void the_second_has_caught_up_and_has_the_vocab() => ReaderOf(TheSecond).Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      }

      public class and_a_unit_of_work_of_the_second_adds_a_vocab_with_the_same_id : after_a_unit_of_work_of_the_first_memory_image_adds_a_vocab_that_a_started_second_has_not_seen
      {
         readonly Exception _thrown;

         public and_a_unit_of_work_of_the_second_adds_a_vocab_with_the_same_id() =>
            _thrown = Invoking(() => InUnitOfWorkOf(TheSecond, taggregates => taggregates.Add(StoredVocab.Create(VocabId, "nomu")))).Must().Throw<Exception>().Which;

         [PCT] public void it_fails_saying_the_store_already_holds_a_taggregate_with_the_id() => _thrown.Message.Must().Contain($"already holds a taggregate {VocabId}");
      }
   }

   public class after_two_memory_images_each_add_cards_to_one_deck_from_many_threads_at_once_and_each_adds_a_sentence : Given_a_memory_image_state_store_kept_in_the_domain_database
   {
      const int CardsEachMemoryImageAdds = 20;
      readonly TaggregateId _deckId = new(Guid.NewGuid());

      protected override async Task InitializeAsyncInternal()
      {
         await base.InitializeAsyncInternal();
         InUnitOfWorkOf(TheFirst, taggregates => taggregates.Add(StoredDeck.Create(_deckId)));
         IDependencyInjectionContainer[] memoryImages = [TheFirst, TheSecond];
         //Each unit of work on a thread of its own, as a test's blocking actors are: forty of them blocked on thread-pool threads
         //starved the pool, and on Linux a synchronous socket read completes only once a pool thread handles the socket's event, so
         //the unit of work holding the deck waited for its database's answer as long as the pool took to grow (2026-11-25).
         var addingTheCards = memoryImages.SelectMany(memoryImage => Enumerable.Range(0, CardsEachMemoryImageAdds).Select(card => TaskCE.RunOnDedicatedThreadAsync(() => AddACard(memoryImage, $"card {card}"))))
                                          .ToArray();
         //They take a second or two. Far longer means a unit of work holding the deck's lock is stalled: the dump shows on what.
         await Task.WhenAll(addingTheCards).WaitAsyncDumpingTheProcessOnTimeout(TimeSpan.FromSeconds(10), $"the {addingTheCards.Length} units of work adding cards to one deck");
         foreach(var memoryImage in memoryImages) HaveAUnitOfWorkOfItChangeTheStore(memoryImage);
      }

      void AddACard(IDependencyInjectionContainer memoryImage, string text) =>
         InUnitOfWorkOf(memoryImage, taggregates => taggregates.GetForUpdate<StoredDeck>(_deckId).AddCard(text));

      [PCT] public void the_first_has_every_card() => ReaderOf(TheFirst).Get<StoredDeck>(_deckId).Cards.InCreationOrder.Count.Must().Be(2 * CardsEachMemoryImageAdds);

      [PCT] public void the_second_has_every_card() => ReaderOf(TheSecond).Get<StoredDeck>(_deckId).Cards.InCreationOrder.Count.Must().Be(2 * CardsEachMemoryImageAdds);

      public class and_a_third_memory_image_starts : after_two_memory_images_each_add_cards_to_one_deck_from_many_threads_at_once_and_each_adds_a_sentence
      {
         IDependencyInjectionContainer _theThird = null!;

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            _theThird = StartAMemoryImage();
         }

         [PCT] public void it_has_every_card() => ReaderOf(_theThird).Get<StoredDeck>(_deckId).Cards.InCreationOrder.Count.Must().Be(2 * CardsEachMemoryImageAdds);
      }
   }
}
