using System.Diagnostics;
using System.Text.Json;
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
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.Taggregates.BaseClasses;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.StateStore;

///<summary>What the end review by a Fable agent found by running code, each as a specification of what the spike does today at 46cc881a6.<br/>
/// These pin the behaviour found, not the behaviour wanted: each passes while the finding stands. They are evidence for the review in<br/>
/// dev_docs/taggregate-improvements/shared/reviews/end-review-by-a-fable-agent.md, not specifications to keep.</summary>
public class Findings_of_the_end_review_by_a_fable_agent : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Converters = { new TaggregateIdAsAGuid() } };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-end-review-fable", Guid.NewGuid().ToString("N")));

   IDependencyInjectionContainer? _container;
   IDependencyInjectionContainer Container => _container!;
   TaggregateId VocabId { get; } = new(Guid.NewGuid());
   List<string> ParticipationLog { get; } = [];
   List<string> ObservationLog { get; } = [];

   void Compose(Action<StateStoreBuilder>? declare = null, Action<TessageBusHandlerRegistrar>? participation = null, Action<TeventObservationRegistrar>? observation = null)
   {
      var builder = TestEnv.DIContainer.CreateTestingContainerBuilder();
      builder.Registrar.StateStore(declare ?? (store => store.Taggregate(() => new StoredVocab()).Taggregate(() => new StoredSentence())));
      builder.Registrar.LocalTessagingEngine(engine =>
      {
         if(participation != null) engine.RegisterTessageBusHandlers(participation);
         if(observation != null) engine.ObserveTevents(observation);
      });
      _container = builder.Build();
   }

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) =>
      Container.ScopeFactory.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.Resolve<ITaggregateStoreUpdater>()));

   ITaggregateStoreReader Reader => Container.RootResolver.Resolve<ITaggregateStoreReader>();

   ITaggregateStoreBulkReader BulkReader => Container.RootResolver.Resolve<ITaggregateStoreBulkReader>();

   void WaitForTheObservers() => Container.RootResolver.Resolve<ILocalTessagingEngine>().AwaitNoTessagesInFlightAsync().GetAwaiter().GetResult();

   static Exception? ExceptionOf(Action action)
   {
      try
      {
         action();
         return null;
      }
#pragma warning disable CA1031 //The specification asserts on whichever exception came.
      catch(Exception exception)
#pragma warning restore CA1031
      {
         return exception;
      }
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null) await _container.DisposeAsync();
   }

   ///<summary>ITaggregateStoreUpdater.TryGetForUpdate documents that the unit of work holds the lock of an id the store doesn't hold,<br/>
   /// "so no other unit of work can add it before this one completes". This one can't add it either: Save takes the same lock again,<br/>
   /// and the lock is a SemaphoreSlim that its own holder can't enter twice.</summary>
   public class when_a_unit_of_work_finds_no_vocab_to_get_for_update_and_then_saves_a_new_vocab_with_that_id : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly Exception? _thrown;
      readonly TimeSpan _waited;

      public when_a_unit_of_work_finds_no_vocab_to_get_for_update_and_then_saves_a_new_vocab_with_that_id()
      {
         Compose();
         var stopwatch = Stopwatch.StartNew();
         _thrown = ExceptionOf(() => InUnitOfWork(taggregates =>
         {
            taggregates.TryGetForUpdate(VocabId, out StoredVocab? _).Must().BeFalse();
            taggregates.Save(StoredVocab.Create(VocabId, "taberu"));
         }));
         _waited = stopwatch.Elapsed;
      }

      [PCT] public void the_save_throws_a_TimeoutException() => (_thrown is TimeoutException).Must().BeTrue();
      [PCT] public void the_message_blames_another_unit_of_work() => _thrown!.Message.Must().Contain("for another unit of work to finish changing taggregate");
      [PCT] public void it_waited_the_locks_30_seconds_first() => (_waited > TimeSpan.FromSeconds(29)).Must().BeTrue();
      [PCT] public void the_store_holds_no_such_vocab() => Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();
   }

   ///<summary>The same lock, taken twice by one unit of work that asks twice for a taggregate the store doesn't hold.</summary>
   public class when_a_unit_of_work_tries_twice_to_get_for_update_a_vocab_the_store_does_not_hold : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly Exception? _thrown;

      public when_a_unit_of_work_tries_twice_to_get_for_update_a_vocab_the_store_does_not_hold()
      {
         Compose();
         _thrown = ExceptionOf(() => InUnitOfWork(taggregates =>
         {
            taggregates.TryGetForUpdate(VocabId, out StoredVocab? _).Must().BeFalse();
            taggregates.TryGetForUpdate(VocabId, out StoredVocab? _).Must().BeFalse();
         }));
      }

      [PCT] public void the_second_try_throws_a_TimeoutException() => (_thrown is TimeoutException).Must().BeTrue();
   }

   ///<summary>A unit of work that saved a new vocab rolls back, and the code tries again with the same instance, as a retry does. The<br/>
   /// first unit of work took the instance's tevents, so the second commits the vocab having dispatched nothing: no participation<br/>
   /// handler ran in the unit of work that committed it, and no observer ever observes its creation. The tevent store refuses such a save.</summary>
   public class when_a_new_vocab_is_saved_again_after_the_unit_of_work_that_saved_it_rolled_back : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly Exception? _thrownByTheSecondSave;

      public when_a_new_vocab_is_saved_again_after_the_unit_of_work_that_saved_it_rolled_back()
      {
         Compose(participation: handle => handle.ForTevent((IStoredVocabCreated it) => ParticipationLog.Add($"created {it.Question}")),
                 observation: observe => observe.ForTevent((IStoredVocabCreated it) => ObservationLog.Add($"created {it.Question}")));
         var vocab = StoredVocab.Create(VocabId, "taberu");

         ExceptionOf(() => InUnitOfWork(taggregates =>
         {
            taggregates.Save(vocab);
            throw new InvalidOperationException("The unit of work fails.");
         })).Must().NotBeNull();
         ParticipationLog.Clear();

         _thrownByTheSecondSave = ExceptionOf(() => InUnitOfWork(taggregates => taggregates.Save(vocab)));
         WaitForTheObservers();
      }

      [PCT] public void the_second_save_throws_nothing() => _thrownByTheSecondSave.Must().BeNull();
      [PCT] public void the_store_holds_the_vocab() => Reader.Get<StoredVocab>(VocabId).Question.Must().Be("taberu");
      [PCT] public void no_participation_handler_ran_in_the_unit_of_work_that_committed_it() => ParticipationLog.Must().BeEmpty();
      [PCT] public void no_observer_ever_observed_its_creation() => ObservationLog.Must().BeEmpty();
   }

   ///<summary>ITaggregateReference is covariant, and the store's own comment says "a reference is compared by id". Two references to<br/>
   /// one taggregate, asked for as two classes of its hierarchy, are not equal, though their hash codes are.</summary>
   public class when_one_vocab_is_referred_to_as_a_taggregate_and_as_a_vocab : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly ITaggregateReference<ITaggregate> _asATaggregate;
      readonly ITaggregateReference<StoredVocab> _asAVocab;

      public when_one_vocab_is_referred_to_as_a_taggregate_and_as_a_vocab()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
         _asATaggregate = Reader.Reference<ITaggregate>(VocabId);
         _asAVocab = Reader.Reference<StoredVocab>(VocabId);
      }

      [PCT] public void both_give_the_same_instance() => ReferenceEquals(_asATaggregate.Current, _asAVocab.Current).Must().BeTrue();
      [PCT] public void their_hash_codes_are_equal() => _asATaggregate.GetHashCode().Must().Be(_asAVocab.GetHashCode());
      [PCT] public void they_are_not_equal() => _asATaggregate.Equals(_asAVocab).Must().BeFalse();
      [PCT] public void a_set_of_references_holds_the_vocab_twice() => new HashSet<ITaggregateReference<ITaggregate>> { _asATaggregate, _asAVocab }.Count.Must().Be(2);

      [PCT] public void the_store_then_makes_a_new_reference_at_each_request_for_it_as_a_vocab() =>
         ReferenceEquals(Reader.Reference<StoredVocab>(VocabId), Reader.Reference<StoredVocab>(VocabId)).Must().BeFalse();

      [PCT] public void and_at_each_read_of_every_vocab() =>
         ReferenceEquals(BulkReader.All<StoredVocab>().Single(), BulkReader.All<StoredVocab>().Single()).Must().BeFalse();
   }

   ///<summary>The reader makes a reference to any id, and the state store keeps each for good.</summary>
   public class when_a_reference_is_asked_for_to_an_id_no_store_holds : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly ITaggregateReference<StoredVocab> _reference;

      public when_a_reference_is_asked_for_to_an_id_no_store_holds()
      {
         Compose();
         _reference = Reader.Reference<StoredVocab>(VocabId);
      }

      [PCT] public void the_reader_gives_one() => _reference.Id.Must().Be(VocabId);
      [PCT] public void reading_its_Current_throws() => ExceptionOf(() => _ = _reference.Current).Must().NotBeNull();
      [PCT] public void the_store_gives_the_same_reference_again() => ReferenceEquals(_reference, Reader.Reference<StoredVocab>(VocabId)).Must().BeTrue();
   }

   ///<summary>ITaggregateStoreReader.TryGet is documented to give false "if no store holds the taggregate". A taggregate of another class<br/>
   /// of the same store is not that: it throws.</summary>
   public class when_trying_to_get_a_vocab_as_a_sentence : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly Exception? _thrown;

      public when_trying_to_get_a_vocab_as_a_sentence()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
         _thrown = ExceptionOf(() => Reader.TryGet(VocabId, out StoredSentence? _));
      }

      [PCT] public void it_throws() => _thrown!.Message.Must().Contain("not a");
   }

   ///<summary>An index is resolved from the container by its interface alone, which is covariant in the taggregate class and contravariant<br/>
   /// in the key. With an index of every taggregate and an index of the vocabs under keys of one type, the first can't be resolved.</summary>
   public class when_a_store_has_an_index_of_every_taggregate_and_an_index_of_the_vocabs_by_keys_of_one_type : Findings_of_the_end_review_by_a_fable_agent
   {
      public when_a_store_has_an_index_of_every_taggregate_and_an_index_of_the_vocabs_by_keys_of_one_type() =>
         Compose(store => store.Taggregate(() => new StoredVocab())
                               .Taggregate(() => new StoredSentence())
                               .Index<ITaggregate, string>(taggregate => [taggregate.GetType().Name])
                               .Index<StoredVocab, string>(vocab => [vocab.Question]));

      [PCT] public void the_index_of_the_vocabs_resolves() => Container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>().Must().NotBeNull();

      [PCT] public void resolving_the_index_of_every_taggregate_throws_that_a_sequence_contains_more_than_one_element() =>
         ExceptionOf(() => Container.RootResolver.Resolve<ITaggregateIndex<ITaggregate, string>>())!.ToString().Must().Contain("more than one");
   }

   ///<summary>Two indexes of one class under keys of one type, such as vocabs by question and by meaning, both strings: an index is<br/>
   /// known by its class and key type alone. Composing fails, with the container's message, which names the interface and says<br/>
   /// nothing of what to do: give each index a key type of its own.</summary>
   public class when_a_store_is_declared_with_two_indexes_of_the_vocabs_by_string : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly Exception? _thrownComposing;

      public when_a_store_is_declared_with_two_indexes_of_the_vocabs_by_string() =>
         _thrownComposing = ExceptionOf(() => Compose(store => store.Taggregate(() => new StoredVocab())
                                                                    .Taggregate(() => new StoredSentence())
                                                                    .Index<StoredVocab, string>(vocab => [vocab.Question])
                                                                    .Index<StoredVocab, string>(vocab => [vocab.Meaning])));

      [PCT] public void composing_fails_saying_only_that_the_service_type_of_the_index_is_already_registered()
      {
         _thrownComposing!.Message.Must().Contain("Service type 'Compze.Teventive.StateStore.ITaggregateIndex`2[[");
         _thrownComposing.Message.Must().Contain("is already registered.");
      }
   }

   ///<summary>A lookup outside a unit of work gives the index's own array of the key's references, typed as a read only collection.</summary>
   public class when_a_caller_casts_what_a_lookup_gives_to_an_array_and_writes_to_it : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly TaggregateId _otherVocabId = new(Guid.NewGuid());

      public when_a_caller_casts_what_a_lookup_gives_to_an_array_and_writes_to_it()
      {
         Compose(store => store.Taggregate(() => new StoredVocab()).Taggregate(() => new StoredSentence()).Index<StoredVocab, string>(vocab => [vocab.Question]));
         InUnitOfWork(taggregates =>
         {
            taggregates.Save(StoredVocab.Create(VocabId, "taberu"));
            taggregates.Save(StoredVocab.Create(_otherVocabId, "kuu"));
         });
         var filedUnderTaberu = (ITaggregateReference<StoredVocab>[])ByQuestion.Lookup("taberu");
         filedUnderTaberu[0] = Reader.Reference<StoredVocab>(_otherVocabId);
      }

      ITaggregateIndex<StoredVocab, string> ByQuestion => Container.RootResolver.Resolve<ITaggregateIndex<StoredVocab, string>>();

      [PCT] public void the_index_now_files_the_other_vocab_under_the_key() => ByQuestion.Lookup("taberu").Single().Id.Must().Be(_otherVocabId);
   }

   ///<summary>IStateStore.ReloadFromStorage on a store composed with no storage, which keeps its taggregates in memory only.</summary>
   public class when_a_store_with_no_storage_reloads_from_its_storage : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly ReloadedTaggregates _reloaded;

      public when_a_store_with_no_storage_reloads_from_its_storage()
      {
         Compose();
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
         _reloaded = Container.RootResolver.Resolve<IStateStore>().ReloadFromStorage();
      }

      [PCT] public void it_reports_the_vocab_removed() => _reloaded.Removed.Must().SequenceEqual([VocabId]);
      [PCT] public void the_store_no_longer_holds_the_vocab() => Reader.TryGet(VocabId, out StoredVocab? _).Must().BeFalse();
   }

   ///<summary>A taggregate's version, and the version each tevent gives it, count from the store's last load: the store doesn't keep<br/>
   /// versions, and a snapshot's restore can't give one, since RestoreIdentity takes only the id.</summary>
   public class when_a_vocab_changed_twice_is_loaded_again_and_changed_once_more : Findings_of_the_end_review_by_a_fable_agent
   {
      readonly int _versionBeforeTheReload;
      readonly int _versionAfterTheReload;

      public when_a_vocab_changed_twice_is_loaded_again_and_changed_once_more()
      {
         Compose(store => store.Taggregate(() => new StoredVocab())
                               .Taggregate(() => new StoredSentence())
                               .StoredInFiles(_folder, Options, files => files.Of<StoredVocab>("vocab").Of<StoredSentence>("sentences").KeptInMemory()),
                 participation: handle => handle.ForTevent((IStoredVocabMeaningChanged it) => ParticipationLog.Add($"{it.Meaning} gave version {it.TaggregateVersion}")));
         InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(VocabId, "taberu")));
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to eat"));
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to consume"));
         _versionBeforeTheReload = Reader.Get<StoredVocab>(VocabId).Version;

         Container.RootResolver.Resolve<IStateStore>().ReloadFromStorage();
         _versionAfterTheReload = Reader.Get<StoredVocab>(VocabId).Version;
         InUnitOfWork(taggregates => taggregates.GetForUpdate<StoredVocab>(VocabId).ChangeMeaning("to devour"));
      }

      [PCT] public void its_version_was_3_before_the_reload() => _versionBeforeTheReload.Must().Be(3);
      [PCT] public void its_version_is_1_after_the_reload() => _versionAfterTheReload.Must().Be(1);

      [PCT] public void two_of_its_tevents_gave_it_version_2() =>
         ParticipationLog.Must().SequenceEqual(["to eat gave version 2", "to consume gave version 3", "to devour gave version 2"]);
   }

   ///<summary>The spike removed Taggregate's protected constructor, so the class, which isn't abstract, now has a public one.</summary>
   public class when_code_outside_a_taggregate_class_constructs_the_Taggregate_base_class_itself : Findings_of_the_end_review_by_a_fable_agent
   {
      [PCT] public void it_compiles_and_throws_when_constructed() =>
         ExceptionOf(() => _ = new Taggregate<StoredVocab, IStoredVocabTevent, StoredVocabTevent, IStoredVocabTevent<IStoredVocabTevent>, StoredVocabTevent<StoredVocabTevent>>())
           .Must().NotBeNull();
   }
}
