using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Teventive.StateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.TeventStore.Abstractions;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.StateStore;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TaggregateStores;

///<summary>A tevent store keeping users and a state store keeping vocab, in one container: one updater, one reader and one bulk reader<br/>
/// serve both.</summary>
public class Given_a_tevent_store_and_a_state_store : UniversalTestBase
{
   readonly IDependencyInjectionContainer _container;
   readonly TaggregateId _userId = new();
   readonly TaggregateId _vocabId = new(Guid.NewGuid());

   public Given_a_tevent_store_and_a_state_store() =>
      _container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings(),
                                                             configureContainer: register => register.StateStore(store => store.Taggregate(() => new StoredVocab())),
                                                             declareTeventStoreTaggregates: store => store.Taggregate<User>());

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync().AsTask();

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   ITaggregateStoreBulkReader BulkReader => _container.RootResolver.Resolve<ITaggregateStoreBulkReader>();

   ITeventStoreReader TeventStoreReader => _container.RootResolver.Resolve<ITeventStoreReader>();

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) => _container.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.TaggregateStoreUpdater()));

   void AddAUserAndAVocab(ITaggregateStoreUpdater taggregates)
   {
      User.Register(taggregates, "registered@example.com", "password", _userId);
      taggregates.Add(StoredVocab.Create(_vocabId, "taberu"));
   }

   public class after_one_unit_of_work_adds_a_user_and_a_vocab : Given_a_tevent_store_and_a_state_store
   {
      public after_one_unit_of_work_adds_a_user_and_a_vocab() => InUnitOfWork(AddAUserAndAVocab);

      [PCT] public void the_reader_gets_the_user() => Reader.Get<User>(_userId).Email.Must().Be("registered@example.com");
      [PCT] public void the_reader_gets_the_vocab() => Reader.Get<StoredVocab>(_vocabId).Question.Must().Be("taberu");
      [PCT] public void publishing_on_the_user_the_reader_gets_throws() => Invoking(() => Reader.Get<User>(_userId).ChangeEmail("changed@example.com")).Must().Throw<InvalidOperationException>();
      [PCT] public void the_bulk_reader_gives_every_user_as_the_added_user() => BulkReader.ReferencesToAll<User>().Select(it => it.Id).Must().SequenceEqual([_userId]);
      [PCT] public void the_bulk_reader_gives_every_vocab_as_the_added_vocab() => BulkReader.ReferencesToAll<StoredVocab>().Select(it => it.Id).Must().SequenceEqual([_vocabId]);
      [PCT] public void the_reference_the_bulk_reader_gives_for_the_user_gives_its_email() => BulkReader.ReferencesToAll<User>().Single().Current.Email.Must().Be("registered@example.com");

      public class and_a_second_unit_of_work_registers_another_user_deletes_the_first_and_asks_for_every_user : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         readonly TaggregateId _otherUserId = new();
         IReadOnlyList<TaggregateId> _everyUserInsideTheUnitOfWork = [];

         public and_a_second_unit_of_work_registers_another_user_deletes_the_first_and_asks_for_every_user() =>
            InUnitOfWork(taggregates =>
            {
               User.Register(taggregates, "other@example.com", "password", _otherUserId);
               taggregates.GetForUpdate<User>(_userId).Delete();
               _everyUserInsideTheUnitOfWork = [..BulkReader.ReferencesToAll<User>().Select(it => it.Id)];
            });

         [PCT] public void inside_the_unit_of_work_every_user_is_the_other_user() => _everyUserInsideTheUnitOfWork.Must().SequenceEqual([_otherUserId]);
         [PCT] public void after_the_commit_every_user_is_the_other_user() => BulkReader.ReferencesToAll<User>().Select(it => it.Id).Must().SequenceEqual([_otherUserId]);
      }

      public class and_a_second_unit_of_work_deletes_both : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         bool _insideTheReaderFoundTheUser = true, _insideTheReaderFoundTheVocab = true;
         bool _insideGettingTheUserForUpdateFoundIt = true, _insideGettingTheVocabForUpdateFoundIt = true;
         IReadOnlyList<TaggregateId> _insideEveryUser = [], _insideEveryVocab = [];

         public and_a_second_unit_of_work_deletes_both() =>
            InUnitOfWork(taggregates =>
            {
               taggregates.GetForUpdate<User>(_userId).Delete();
               taggregates.GetForUpdate<StoredVocab>(_vocabId).Delete();
               _insideTheReaderFoundTheUser = Reader.TryGet(_userId, out User? _);
               _insideTheReaderFoundTheVocab = Reader.TryGet(_vocabId, out StoredVocab? _);
               _insideGettingTheUserForUpdateFoundIt = taggregates.TryGetForUpdate(_userId, out User? _);
               _insideGettingTheVocabForUpdateFoundIt = taggregates.TryGetForUpdate(_vocabId, out StoredVocab? _);
               _insideEveryUser = [..BulkReader.ReferencesToAll<User>().Select(it => it.Id)];
               _insideEveryVocab = [..BulkReader.ReferencesToAll<StoredVocab>().Select(it => it.Id)];
            });

         [PCT] public void inside_the_unit_of_work_the_reader_found_no_user() => _insideTheReaderFoundTheUser.Must().BeFalse();
         [PCT] public void inside_the_unit_of_work_the_reader_found_no_vocab() => _insideTheReaderFoundTheVocab.Must().BeFalse();
         [PCT] public void inside_the_unit_of_work_getting_the_user_for_update_found_nothing() => _insideGettingTheUserForUpdateFoundIt.Must().BeFalse();
         [PCT] public void inside_the_unit_of_work_getting_the_vocab_for_update_found_nothing() => _insideGettingTheVocabForUpdateFoundIt.Must().BeFalse();
         [PCT] public void inside_the_unit_of_work_every_user_was_none() => _insideEveryUser.Must().BeEmpty();
         [PCT] public void inside_the_unit_of_work_every_vocab_was_none() => _insideEveryVocab.Must().BeEmpty();
         [PCT] public void the_reader_gets_no_user() => Reader.TryGet(_userId, out User? _).Must().BeFalse();
         [PCT] public void the_reader_gets_no_vocab() => Reader.TryGet(_vocabId, out StoredVocab? _).Must().BeFalse();
         [PCT] public void getting_the_user_throws() => Invoking(() => Reader.Get<User>(_userId)).Must().Throw<Exception>();
         [PCT] public void the_bulk_reader_gives_no_user() => BulkReader.ReferencesToAll<User>().Must().BeEmpty();
         [PCT] public void the_bulk_reader_gives_no_vocab() => BulkReader.ReferencesToAll<StoredVocab>().Must().BeEmpty();
         [PCT] public void the_tevent_store_keeps_the_users_history_ending_with_the_deleted_tevent() => TeventStoreReader.GetHistory(_userId).Select(it => it.Tevent.GetType().Name).Must().SequenceEqual([nameof(UserRegistered), nameof(UserDeleted)]);
         [PCT] public void the_tevent_store_gives_the_users_version_before_the_deletion() => TeventStoreReader.GetReadonlyCopyOfVersion<User>(_userId, 1).Email.Must().Be("registered@example.com");
      }

      public class and_a_second_unit_of_work_deletes_both_and_then_fails : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         public and_a_second_unit_of_work_deletes_both_and_then_fails() =>
            Invoking(() => InUnitOfWork(taggregates =>
            {
               taggregates.GetForUpdate<User>(_userId).Delete();
               taggregates.GetForUpdate<StoredVocab>(_vocabId).Delete();
               throw new InvalidOperationException("The unit of work fails.");
            })).Must().Throw<InvalidOperationException>();

         [PCT] public void the_reader_still_gets_the_user() => Reader.Get<User>(_userId).Email.Must().Be("registered@example.com");
         [PCT] public void the_reader_still_gets_the_vocab() => Reader.Get<StoredVocab>(_vocabId).Question.Must().Be("taberu");
         [PCT] public void the_tevent_store_keeps_the_users_history_without_the_deleted_tevent() => TeventStoreReader.GetHistory(_userId).Select(it => it.Tevent.GetType().Name).Must().SequenceEqual([nameof(UserRegistered)]);
      }

      public class and_a_second_unit_of_work_deletes_the_user_and_then_changes_its_email : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         readonly Exception _exception;

         public and_a_second_unit_of_work_deletes_the_user_and_then_changes_its_email() =>
            _exception = Invoking(() => InUnitOfWork(taggregates =>
            {
               var user = taggregates.GetForUpdate<User>(_userId);
               user.Delete();
               user.ChangeEmail("changed@example.com");
            })).Must().Throw<Exception>().Which;

         [PCT] public void the_exception_names_the_user_and_the_tevent() => _exception.Message.Must().Satisfy(it => it.Contains(_userId.ToString(), StringComparison.Ordinal) && it.Contains(nameof(UserChangedEmail), StringComparison.Ordinal));
         [PCT] public void the_reader_gets_the_user_as_it_was() => Reader.Get<User>(_userId).Email.Must().Be("registered@example.com");
      }

      public class and_a_second_unit_of_work_deletes_the_vocab_and_then_changes_its_meaning : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         readonly Exception _exception;

         public and_a_second_unit_of_work_deletes_the_vocab_and_then_changes_its_meaning() =>
            _exception = Invoking(() => InUnitOfWork(taggregates =>
            {
               var vocab = taggregates.GetForUpdate<StoredVocab>(_vocabId);
               vocab.Delete();
               vocab.ChangeMeaning("to eat");
            })).Must().Throw<Exception>().Which;

         [PCT] public void the_exception_names_the_vocab_and_the_tevent() => _exception.Message.Must().Satisfy(it => it.Contains(_vocabId.ToString(), StringComparison.Ordinal) && it.Contains(nameof(StoredVocabMeaningChanged), StringComparison.Ordinal));
         [PCT] public void the_reader_gets_the_vocab_as_it_was() => Reader.Get<StoredVocab>(_vocabId).Question.Must().Be("taberu");
      }

      public class and_a_second_unit_of_work_changes_both : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         public and_a_second_unit_of_work_changes_both() =>
            InUnitOfWork(taggregates =>
            {
               taggregates.GetForUpdate<User>(_userId).ChangeEmail("changed@example.com");
               taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaning("to eat");
            });

         [PCT] public void the_reader_gets_the_users_new_email() => Reader.Get<User>(_userId).Email.Must().Be("changed@example.com");
         [PCT] public void the_reader_gets_the_vocabs_new_meaning() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("to eat");
      }

      public class and_a_second_unit_of_work_changes_both_and_then_fails : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         public and_a_second_unit_of_work_changes_both_and_then_fails() =>
            Invoking(() => InUnitOfWork(taggregates =>
            {
               taggregates.GetForUpdate<User>(_userId).ChangeEmail("changed@example.com");
               taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaning("to eat");
               throw new InvalidOperationException("The unit of work fails.");
            })).Must().Throw<InvalidOperationException>();

         [PCT] public void the_reader_gets_the_users_email_from_before() => Reader.Get<User>(_userId).Email.Must().Be("registered@example.com");
         [PCT] public void the_reader_gets_the_vocabs_meaning_from_before() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("");
      }

      public class and_a_second_unit_of_work_gets_the_user_for_update_and_then_reads_it : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         User? _gotForUpdate;
         User? _read;

         public and_a_second_unit_of_work_gets_the_user_for_update_and_then_reads_it() =>
            InUnitOfWork(taggregates =>
            {
               _gotForUpdate = taggregates.GetForUpdate<User>(_userId);
               _gotForUpdate.ChangeEmail("changed@example.com");
               _read = Reader.Get<User>(_userId);
            });

         [PCT] public void the_reader_got_the_instance_the_unit_of_work_got_for_update() => _read.Must().ReferenceEqual(_gotForUpdate);
      }

      public class and_a_second_unit_of_work_gets_the_user_for_update_and_then_gets_it_for_update_in_a_scope_of_its_own : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         User? _gotForUpdate;
         User? _gotInTheOtherScope;

         public and_a_second_unit_of_work_gets_the_user_for_update_and_then_gets_it_for_update_in_a_scope_of_its_own() =>
            InUnitOfWork(taggregates =>
            {
               _gotForUpdate = taggregates.GetForUpdate<User>(_userId);
               _gotInTheOtherScope = _container.ExecuteInIsolatedScope(scope => scope.TaggregateStoreUpdater().GetForUpdate<User>(_userId));
            });

         [PCT] public void the_other_scope_got_the_instance_the_unit_of_work_got_for_update() => _gotInTheOtherScope.Must().ReferenceEqual(_gotForUpdate);
      }
   }

   public class after_three_units_of_work_each_register_a_user : Given_a_tevent_store_and_a_state_store
   {
      readonly TaggregateId[] _userIds = [new(), new(), new()];

      public after_three_units_of_work_each_register_a_user()
      {
         foreach(var id in _userIds)
            InUnitOfWork(taggregates => User.Register(taggregates, $"{id}@example.com", "password", id));
      }

      [PCT] public void the_bulk_reader_gives_the_three_users() => BulkReader.ReferencesToAll<User>().Select(it => it.Id).ToList().Must().Satisfy(it => it.Count == 3 && _userIds.All(it.Contains));
   }

   public class after_a_unit_of_work_that_adds_a_user_and_a_vocab_fails : Given_a_tevent_store_and_a_state_store
   {
      public after_a_unit_of_work_that_adds_a_user_and_a_vocab_fails() =>
         Invoking(() => InUnitOfWork(taggregates =>
         {
            AddAUserAndAVocab(taggregates);
            throw new InvalidOperationException("The unit of work fails.");
         })).Must().Throw<InvalidOperationException>();

      [PCT] public void the_reader_gets_no_user() => Reader.TryGet(_userId, out User? _).Must().BeFalse();
      [PCT] public void the_reader_gets_no_vocab() => Reader.TryGet(_vocabId, out StoredVocab? _).Must().BeFalse();
   }
}
