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
using Compze.Teventive.StateStore;
using Compze.Teventive.StateStore.Storage;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>A state store whose storage, the consumer's, holds one vocab, "taberu", when the container is built. The storage keeps<br/>
/// the snapshots it is given, as a consumer's files would, and records what the store asks of it.</summary>
public class Given_a_state_store_whose_storage_holds_a_vocab : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   TaggregateId VocabId { get; } = new(Guid.NewGuid());
   RecordingStorage Storage { get; }
   ConcurrentQueue<string> ObservationLog { get; } = new();

   protected Given_a_state_store_whose_storage_holds_a_vocab()
   {
      Storage = new RecordingStorage(new TaggregateSnapshot(typeof(StoredVocab), VocabId, new StoredVocabSnapshot(VocabId, "taberu", "to eat")));
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.StateStore(store => store.StoredIn(Storage)
                                                 .Taggregate(() => new StoredVocab())
                                                 .Index<StoredVocab, string>(vocab => [vocab.Question]));
      builder.Registrar.LocalTessagingEngine(engine => engine.ObserveTevents(observe => observe.ForTevent((IStoredVocabTevent it) => ObservationLog.Enqueue(it.GetType().Name))));
      _container = builder.Build();
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      _container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   StoredVocab CommittedVocab => Reader.Get<StoredVocab>(VocabId);

   void WaitForTheObservers() => _container.RootResolver.Resolve<ILocalTessagingEngine>().StopAsync().GetAwaiter().GetResult();

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync();

   public class when_the_store_is_first_used : Given_a_state_store_whose_storage_holds_a_vocab
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
   }

   public class after_a_unit_of_work_changes_the_vocabs_meaning : Given_a_state_store_whose_storage_holds_a_vocab
   {
      public after_a_unit_of_work_changes_the_vocabs_meaning() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to dine"));

      [PCT] public void the_storage_holds_the_new_meaning() => Storage.StoredVocabs.Select(it => it.Meaning).Must().SequenceEqual(["to dine"]);
      [PCT] public void the_storage_was_asked_to_write_the_vocab_and_delete_nothing() => Storage.Writes.Must().SequenceEqual(["committed: save StoredVocab"]);
   }

   public class after_a_unit_of_work_saves_a_new_vocab : Given_a_state_store_whose_storage_holds_a_vocab
   {
      public after_a_unit_of_work_saves_a_new_vocab() => InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(new TaggregateId(Guid.NewGuid()), "kuu")));

      [PCT] public void the_storage_holds_both_vocabs() => Storage.StoredVocabs.Select(it => it.Question).Order().Must().SequenceEqual(["kuu", "taberu"]);
   }

   public class after_a_unit_of_work_deletes_the_vocab : Given_a_state_store_whose_storage_holds_a_vocab
   {
      public after_a_unit_of_work_deletes_the_vocab() => InUnitOfWork(taggregates => taggregates.Delete<StoredVocab>(VocabId));

      [PCT] public void the_storage_holds_no_vocab() => Storage.StoredVocabs.Must().BeEmpty();
      [PCT] public void the_storage_was_asked_to_delete_the_vocab() => Storage.Writes.Must().SequenceEqual(["committed: delete StoredVocab"]);
   }

   public class after_a_unit_of_work_gets_the_vocab_for_update_and_changes_nothing : Given_a_state_store_whose_storage_holds_a_vocab
   {
      public after_a_unit_of_work_gets_the_vocab_for_update_and_changes_nothing() => InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId));

      [PCT] public void the_storage_was_asked_to_write_nothing() => Storage.Writes.Must().SequenceEqual(["committed: nothing"]);
   }

   public class when_the_storage_fails_to_prepare_the_write_of_a_change : Given_a_state_store_whose_storage_holds_a_vocab
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

   public class when_another_participant_of_the_transaction_rolls_it_back_after_the_store_has_prepared : Given_a_state_store_whose_storage_holds_a_vocab
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
