using System.Collections.Concurrent;
using System.Transactions;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Wiring;
using Compze.Tessaging.TessageBus;
using Compze.Teventive;
using Compze.Teventive.StateStore;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>A state store whose storage, the consumer's, holds one vocab, "taberu", and one sentence, when the container is built.<br/>
/// The storage keeps the snapshots it is given, as a consumer's files would, and records what the store asks of it. It is a service<br/>
/// of the container, which the store is declared to be stored in. Sentences are declared with a comparison of their snapshots,<br/>
/// which have no Equals of their own.</summary>
public class Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   TaggregateId VocabId { get; } = new(Guid.NewGuid());
   TaggregateId SentenceId { get; } = new(Guid.NewGuid());
   RecordingStorage Storage { get; }
   bool StorageWasResolved { get; set; }
   ConcurrentQueue<string> ObservationLog { get; } = new();

   protected Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence()
   {
      Storage = new RecordingStorage(new TaggregateSnapshot(typeof(StoredVocab), VocabId, new StoredVocabSnapshot(VocabId, "taberu", "to eat")),
                                     new TaggregateSnapshot(typeof(StoredSentence), SentenceId, new StoredSentenceSnapshot(SentenceId, "taberu yo")));
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.Register(Singleton.For<RecordingStorage>().CreatedBy(() =>
      {
         StorageWasResolved = true;
         return Storage;
      }));
      builder.Registrar.StateStore(store => store.StoredIn((RecordingStorage storage) => storage)
                                                 .Taggregate(() => new StoredVocab())
                                                 .Taggregate(() => new StoredSentence(), snapshotsAreEqual: (one, other) => one.Text == other.Text)
                                                 .Index<StoredVocab, string>(vocab => [vocab.Question]));
      builder.Registrar.LocalTessagingEngine(engine => engine.ObserveTevents(observe => observe.ForTevent((IStoredVocabTevent it) => ObservationLog.Enqueue(it.GetType().Name))));
      _container = builder.Build();
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   StoredVocab CommittedVocab => Reader.Get<StoredVocab>(VocabId);

   IStateStore StateStore => _container.RootResolver.Resolve<IStateStore>();

   ITaggregateStoreBulkReader BulkReader => _container.RootResolver.Resolve<ITaggregateStoreBulkReader>();

   IReadOnlyList<TaggregateId> IdsFiledUnder(string question) => [.._container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Lookup(question).Select(it => it.Id)];

   void WaitForTheObservers() => _container.RootResolver.Resolve<ILocalTessagingEngine>().AwaitNoTeventsInFlightAsync().GetAwaiter().GetResult();

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class when_the_container_is_built : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      [PCT] public void the_storage_is_not_yet_resolved() => StorageWasResolved.Must().BeFalse();
   }

   public class when_the_store_is_first_used : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public when_the_store_is_first_used()
      {
         _ = CommittedVocab;
         WaitForTheObservers();
      }

      [PCT] public void the_reader_gets_the_stored_vocab() => new { CommittedVocab.Question, CommittedVocab.Meaning }.Must().DeepEqual(new { Question = "taberu", Meaning = "to eat" });
      [PCT] public void the_index_files_the_stored_vocab_under_its_question() => _container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Lookup("taberu").Select(it => it.Id).Must().SequenceEqual([VocabId]);
      [PCT] public void no_tevent_was_dispatched() => ObservationLog.Must().BeEmpty();
      [PCT] public void the_storage_was_loaded_once() => Storage.LoadCount.Must().Be(1);
      [PCT] public void the_storage_has_been_resolved() => StorageWasResolved.Must().BeTrue();
   }

   public class when_the_storage_also_holds_vocabs_with_one_question_out_of_id_order_and_the_store_is_first_used : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      //Five, held in an order that is neither theirs nor, in all likelihood, the order of their hashes.
      readonly TaggregateId[] _inIdOrder = [new(Guid.Parse("1b4e28ba-2fa1-11d2-883f-0016d3cca427")),
                                            new(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301")),
                                            new(Guid.Parse("8c9d7a1e-5b3f-4e2a-9d1c-7f6e5a4b3c2d")),
                                            new(Guid.Parse("c0ffee00-1234-4abc-8def-0123456789ab")),
                                            new(Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"))];

      public when_the_storage_also_holds_vocabs_with_one_question_out_of_id_order_and_the_store_is_first_used()
      {
         foreach(var id in new[] { _inIdOrder[2], _inIdOrder[4], _inIdOrder[0], _inIdOrder[3], _inIdOrder[1] })
            Storage.Hold(new TaggregateSnapshot(typeof(StoredVocab), id, new StoredVocabSnapshot(id, "kuu", "to eat, rudely")));
      }

      [PCT] public void the_lookup_gives_them_in_id_order() => IdsFiledUnder("kuu").Must().SequenceEqual(_inIdOrder);

      [PCT] public void the_bulk_reader_gives_every_vocab_in_the_order_of_the_text_of_their_ids() =>
         BulkReader.All<StoredVocab>().Select(it => it.Id).Must().SequenceEqual([.._inIdOrder.Append(VocabId).OrderBy(it => it.Value.ToString(), StringComparer.Ordinal)]);
   }

   public class when_asked_for_every_taggregate_of_a_class : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      [PCT] public void every_vocab_is_the_stored_vocab() => BulkReader.All<StoredVocab>().Select(it => it.Id).Must().SequenceEqual([VocabId]);
      [PCT] public void every_sentence_is_the_stored_sentence() => BulkReader.All<StoredSentence>().Select(it => it.Id).Must().SequenceEqual([SentenceId]);
      [PCT] public void every_taggregate_is_the_vocab_and_the_sentence() => BulkReader.All<ITaggregate>().Select(it => it.Id).ToList().Must().Satisfy(it => it.Count == 2 && it.Contains(VocabId) && it.Contains(SentenceId));
      [PCT] public void the_reference_to_the_stored_vocab_gives_it() => BulkReader.All<StoredVocab>().Single().Current.Question.Must().Be("taberu");
   }

   public class when_a_unit_of_work_that_saved_a_new_vocab_and_deleted_the_stored_one_asks_for_every_vocab : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly TaggregateId _newVocabId = new(Guid.NewGuid());
      IReadOnlyList<TaggregateId> _everyVocabInsideTheUnitOfWork = [];

      public when_a_unit_of_work_that_saved_a_new_vocab_and_deleted_the_stored_one_asks_for_every_vocab() =>
         InUnitOfWork(taggregates =>
         {
            taggregates.Save(StoredVocab.Create(_newVocabId, "kuu"));
            taggregates.Delete<StoredVocab>(VocabId);
            _everyVocabInsideTheUnitOfWork = [..BulkReader.All<StoredVocab>().Select(it => it.Id)];
         });

      [PCT] public void inside_the_unit_of_work_every_vocab_is_the_new_one() => _everyVocabInsideTheUnitOfWork.Must().SequenceEqual([_newVocabId]);
      [PCT] public void after_the_commit_every_vocab_is_the_new_one() => BulkReader.All<StoredVocab>().Select(it => it.Id).Must().SequenceEqual([_newVocabId]);
   }

   public class after_a_unit_of_work_changes_the_vocabs_meaning : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public after_a_unit_of_work_changes_the_vocabs_meaning() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine"));

      [PCT] public void the_storage_holds_the_new_meaning() => Storage.StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to dine"]);
      [PCT] public void the_storage_was_asked_to_write_the_vocab_and_delete_nothing() => Storage.Writes.Must().SequenceEqual(["committed: save StoredVocab"]);
   }

   public class after_a_unit_of_work_saves_a_new_vocab : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public after_a_unit_of_work_saves_a_new_vocab() => InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), "kuu")));

      [PCT] public void the_storage_holds_both_vocabs() => Storage.StoredVocabs.Select(it => it.Question).Order().Must().SequenceEqual(["kuu", "taberu"]);
   }

   public class after_a_unit_of_work_deletes_the_vocab : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public after_a_unit_of_work_deletes_the_vocab() => InUnitOfWork(taggregates => taggregates.Delete<StoredVocab>(VocabId));

      [PCT] public void the_storage_holds_no_vocab() => Storage.StoredVocabs.Must().BeEmpty();
      [PCT] public void the_storage_was_asked_to_delete_the_vocab() => Storage.Writes.Must().SequenceEqual(["committed: delete StoredVocab"]);
   }

   public class after_a_unit_of_work_gets_the_vocab_for_update_and_changes_nothing : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public after_a_unit_of_work_gets_the_vocab_for_update_and_changes_nothing() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId));

      [PCT] public void the_storage_was_asked_to_write_nothing() => Storage.Writes.Must().SequenceEqual(["committed: nothing"]);
   }

   public class when_the_storage_fails_to_prepare_the_write_of_a_change : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly Exception _exception;

      public when_the_storage_fails_to_prepare_the_write_of_a_change()
      {
         Storage.FailToPrepare = true;
         _exception = Invoking(() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine")))
                     .Must().Throw<TransactionAbortedException>().Which;
      }

      [PCT] public void the_unit_of_work_rolls_back_with_the_storages_exception() => _exception.InnerException!.Message.Must().Be(RecordingStorage.PrepareFailure);
      [PCT] public void the_reader_gets_the_meaning_committed_before() => CommittedVocab.Meaning.Must().Be("to eat");
      [PCT] public void the_storage_still_holds_the_meaning_committed_before() => Storage.StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to eat"]);
   }

   public class when_another_participant_of_the_transaction_rolls_it_back_after_the_store_has_prepared : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public when_another_participant_of_the_transaction_rolls_it_back_after_the_store_has_prepared() =>
         Invoking(() => InUnitOfWork(taggregates =>
                 {
                    taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine");
                    Transaction.Current!.EnlistVolatile(new ParticipantThatRollsBack(), EnlistmentOptions.None);
                 }))
           .Must().Throw<TransactionAbortedException>();

      [PCT] public void the_storage_discarded_the_prepared_write() => Storage.Writes.Must().SequenceEqual(["discarded: save StoredVocab"]);
      [PCT] public void the_reader_gets_the_meaning_committed_before() => CommittedVocab.Meaning.Must().Be("to eat");
   }

   public class after_the_storage_changes_the_vocab_and_gets_another_and_the_store_reloads : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly TaggregateId _addedVocabId = new(Guid.NewGuid());
      readonly ITaggregateReference<StoredVocab> _referenceHeldThroughout;
      readonly ReloadedTaggregates _reloaded;

      public after_the_storage_changes_the_vocab_and_gets_another_and_the_store_reloads()
      {
         _referenceHeldThroughout = Reader.Reference<StoredVocab>(VocabId);
         _ = _referenceHeldThroughout.Current;
         Storage.Hold(new TaggregateSnapshot(typeof(StoredVocab), VocabId, new StoredVocabSnapshot(VocabId, "kuu", "to eat, rudely")));
         Storage.Hold(new TaggregateSnapshot(typeof(StoredVocab), _addedVocabId, new StoredVocabSnapshot(_addedVocabId, "nomu", "to drink")));
         _reloaded = StateStore.ReloadFromStorage();
         WaitForTheObservers();
      }

      [PCT] public void the_reload_reports_the_vocab_as_changed() => _reloaded.Changed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_reload_reports_the_other_vocab_as_added() => _reloaded.Added.Must().SequenceEqual([_addedVocabId]);
      [PCT] public void the_reload_reports_none_removed() => _reloaded.Removed.Must().BeEmpty();
      [PCT] public void the_reader_gets_the_changed_vocab() => new { CommittedVocab.Question, CommittedVocab.Meaning }.Must().DeepEqual(new { Question = "kuu", Meaning = "to eat, rudely" });
      [PCT] public void the_reference_held_throughout_gives_the_changed_vocab() => _referenceHeldThroughout.Current.Question.Must().Be("kuu");
      [PCT] public void the_index_files_the_vocab_under_its_new_question() => IdsFiledUnder("kuu").Must().SequenceEqual([VocabId]);
      [PCT] public void the_index_files_nothing_under_its_old_question() => IdsFiledUnder("taberu").Must().BeEmpty();
      [PCT] public void no_tevent_was_dispatched() => ObservationLog.Must().BeEmpty();
   }

   public class after_the_storage_changes_the_sentence_and_the_store_reloads : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly ReloadedTaggregates _reloaded;

      public after_the_storage_changes_the_sentence_and_the_store_reloads()
      {
         _ = CommittedVocab;
         Storage.Hold(new TaggregateSnapshot(typeof(StoredSentence), SentenceId, new StoredSentenceSnapshot(SentenceId, "taberu ne")));
         _reloaded = StateStore.ReloadFromStorage();
      }

      [PCT] public void the_reload_reports_the_sentence_as_changed_by_the_comparison_declared_with_its_class() => _reloaded.Changed.Must().SequenceEqual([SentenceId]);
   }

   public class after_the_storage_drops_the_vocab_and_the_store_reloads : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly ReloadedTaggregates _reloaded;

      public after_the_storage_drops_the_vocab_and_the_store_reloads()
      {
         _ = CommittedVocab;
         Storage.Drop(VocabId);
         _reloaded = StateStore.ReloadFromStorage();
      }

      [PCT] public void the_reload_reports_the_vocab_as_removed() => _reloaded.Removed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_reader_finds_no_vocab() => Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();
      [PCT] public void the_index_files_nothing_under_its_question() => IdsFiledUnder("taberu").Must().BeEmpty();
   }

   public class when_the_store_reloads_while_a_unit_of_work_on_another_thread_changes_the_vocab : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly ManualResetEventSlim _theUnitOfWorkHasTheVocab = new();
      readonly ReloadedTaggregates _reloaded;

      public when_the_store_reloads_while_a_unit_of_work_on_another_thread_changes_the_vocab()
      {
         var unitOfWork = Task.Run(() => InUnitOfWork(taggregates =>
         {
            var vocab = taggregates.GetForUpdate<StoredVocab>(VocabId);
            _theUnitOfWorkHasTheVocab.Set();
            Thread.Sleep(200); //Gives the reload time to begin waiting while the unit of work is open.
            vocab.ChangeMeaning("to dine");
         }));
         _theUnitOfWorkHasTheVocab.Wait();
         _reloaded = StateStore.ReloadFromStorage();
         unitOfWork.Wait();
      }

      [PCT] public void the_reload_waited_for_the_unit_of_work_so_it_reports_no_change() => _reloaded.Changed.Must().BeEmpty();
      [PCT] public void the_reader_gets_what_the_unit_of_work_committed() => CommittedVocab.Meaning.Must().Be("to dine");
   }

   public class after_the_store_reloads_after_a_change_that_changes_the_vocab_in_the_storage : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly ReloadedTaggregates _reloaded;

      public after_the_store_reloads_after_a_change_that_changes_the_vocab_in_the_storage()
      {
         _ = CommittedVocab;
         _reloaded = StateStore.ReloadFromStorageAfter(() => Storage.Hold(new TaggregateSnapshot(typeof(StoredVocab), VocabId, new StoredVocabSnapshot(VocabId, "kuu", "to eat, rudely"))));
      }

      [PCT] public void the_reload_reports_the_vocab_as_changed() => _reloaded.Changed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_reader_gets_the_changed_vocab() => CommittedVocab.Question.Must().Be("kuu");
   }

   public class when_a_unit_of_work_on_another_thread_begins_while_the_change_before_a_reload_runs : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      public when_a_unit_of_work_on_another_thread_begins_while_the_change_before_a_reload_runs()
      {
         _ = CommittedVocab;
         Task? unitOfWork = null;
         StateStore.ReloadFromStorageAfter(() =>
         {
            Storage.Hold(new TaggregateSnapshot(typeof(StoredVocab), VocabId, new StoredVocabSnapshot(VocabId, "kuu", "to eat, rudely")));
            unitOfWork = Task.Run(() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine")));
            Thread.Sleep(200); //Gives the unit of work time to begin, so that it would write the vocab committed before over the change, did it not wait.
         });
         unitOfWork!.Wait();
      }

      [PCT] public void the_unit_of_work_changed_the_vocab_as_reloaded_so_the_storage_keeps_the_change() =>
         Storage.StoredVocabs.Select(it => new { it.Question, it.Meaning }).Must().SequenceEqual([new { Question = "kuu", Meaning = "to dine" }]);
   }

   public class when_the_change_before_a_reload_throws : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      readonly Exception _exception;

      public when_the_change_before_a_reload_throws()
      {
         _ = CommittedVocab;
         _exception = Invoking(() => StateStore.ReloadFromStorageAfter(() =>
                                    {
                                       Storage.Hold(new TaggregateSnapshot(typeof(StoredVocab), VocabId, new StoredVocabSnapshot(VocabId, "kuu", "to eat, rudely")));
                                       throw new InvalidOperationException("The change fails.");
                                    }))
                     .Must().Throw<InvalidOperationException>().Which;
      }

      [PCT] public void the_changes_exception_comes_out() => _exception.Message.Must().Be("The change fails.");
      [PCT] public void nothing_is_reloaded() => CommittedVocab.Question.Must().Be("taberu");


      public class and_a_unit_of_work_then_changes_the_vocab : when_the_change_before_a_reload_throws
      {
         public and_a_unit_of_work_then_changes_the_vocab() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine"));

         [PCT] public void the_reader_gets_the_change() => CommittedVocab.Meaning.Must().Be("to dine");
      }
   }

   public class when_a_unit_of_work_that_changed_the_vocab_reloads_the_store : Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence
   {
      [PCT] public void the_reload_throws() =>
         Invoking(() => InUnitOfWork(taggregates =>
         {
            taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine");
            StateStore.ReloadFromStorage();
         })).Must().Throw<Exception>();
   }

   ///<summary>A transaction participant that votes to roll back. It prepares after the store, which prepares in the transaction's<br/>
   /// first phase.</summary>
   class ParticipantThatRollsBack : IEnlistmentNotification
   {
      public void Prepare(PreparingEnlistment preparingEnlistment) => preparingEnlistment.ForceRollback();
      public void Commit(Enlistment enlistment) => enlistment.Done();
      public void Rollback(Enlistment enlistment) => enlistment.Done();
      public void InDoubt(Enlistment enlistment) => enlistment.Done();
   }

   ///<summary>A consumer's storage, in memory: it keeps the snapshots it is given, and records each write it is asked for, as<br/>
   /// "committed:" or "discarded:" followed by what the write was.</summary>
   class RecordingStorage(params TaggregateSnapshot[] stored) : ITaggregateStorage
   {
      public const string PrepareFailure = "The storage fails to prepare the write.";

      readonly ConcurrentDictionary<TaggregateId, TaggregateSnapshot> _stored = new(stored.ToDictionary(it => it.Id));
      readonly ConcurrentQueue<string> _writes = new();

      public bool FailToPrepare { get; set; }
      public int LoadCount { get; private set; }
      public IReadOnlyList<string> Writes => [.._writes];
      public IReadOnlyList<StoredVocabSnapshot> StoredVocabs => [.._stored.Values.Select(it => it.Snapshot).OfType<StoredVocabSnapshot>()];

      ///<summary>Holds <paramref name="snapshot"/>, as when another program writes a taggregate's file.</summary>
      public void Hold(TaggregateSnapshot snapshot) => _stored[snapshot.Id] = snapshot;

      ///<summary>Holds the taggregate <paramref name="id"/> no longer, as when another program deletes its file.</summary>
      public void Drop(TaggregateId id) => _stored.TryRemove(id, out _);

      public IReadOnlyList<TaggregateSnapshot> LoadAll()
      {
         LoadCount++;
         return [.._stored.Values];
      }

      public IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateToDelete> toDelete)
      {
         if(FailToPrepare) throw new IOException(PrepareFailure);
         return new PreparedWrite(this, toSave, toDelete);
      }

      class PreparedWrite(RecordingStorage storage, IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateToDelete> toDelete) : IPreparedWrite
      {
         readonly RecordingStorage _storage = storage;
         readonly IReadOnlyList<TaggregateSnapshot> _toSave = toSave;
         readonly IReadOnlyList<TaggregateToDelete> _toDelete = toDelete;

         public void Commit()
         {
            foreach(var snapshot in _toSave) _storage._stored[snapshot.Id] = snapshot;
            foreach(var deleted in _toDelete) _storage._stored.TryRemove(deleted.Id, out _);
            _storage._writes.Enqueue($"committed: {Description}");
         }

         public void Discard() => _storage._writes.Enqueue($"discarded: {Description}");

         string Description =>
            _toSave.Count + _toDelete.Count == 0
               ? "nothing"
               : string.Join(", ", [.._toSave.Select(it => $"save {it.TaggregateClass.Name}"), .._toDelete.Select(it => $"delete {it.TaggregateClass.Name}")]);
      }
   }
}
