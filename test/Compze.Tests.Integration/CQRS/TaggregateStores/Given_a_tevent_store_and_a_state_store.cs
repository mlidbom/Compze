using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Must;
using Compze.Teventive.StateStore;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.StateStore;
using static Compze.Must.MustActions;

// ReSharper disable InconsistentNaming for testing
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TaggregateStores;

///<summary>A tevent store keeping users and a state store keeping vocab, in one container: one updater and one reader serve both.</summary>
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

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) => _container.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.TaggregateStoreUpdater()));

   void SaveAUserAndAVocab(ITaggregateStoreUpdater taggregates)
   {
      User.Register(taggregates, "registered@example.com", "password", _userId);
      taggregates.Save(StoredVocab.Create(_vocabId, "taberu"));
   }

   public class after_one_unit_of_work_saves_a_user_and_a_vocab : Given_a_tevent_store_and_a_state_store
   {
      public after_one_unit_of_work_saves_a_user_and_a_vocab() => InUnitOfWork(SaveAUserAndAVocab);

      [PCT] public void the_reader_gets_the_user() => Reader.Get<User>(_userId).Email.Must().Be("registered@example.com");
      [PCT] public void the_reader_gets_the_vocab() => Reader.Get<StoredVocab>(_vocabId).Question.Must().Be("taberu");
      [PCT] public void publishing_on_the_user_the_reader_gets_throws() => Invoking(() => Reader.Get<User>(_userId).ChangeEmail("changed@example.com")).Must().Throw<InvalidOperationException>();

      public class and_a_second_unit_of_work_changes_both : after_one_unit_of_work_saves_a_user_and_a_vocab
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

      public class and_a_second_unit_of_work_changes_both_and_then_fails : after_one_unit_of_work_saves_a_user_and_a_vocab
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

      public class and_a_second_unit_of_work_gets_the_user_for_update_and_then_reads_it : after_one_unit_of_work_saves_a_user_and_a_vocab
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

      public class and_a_second_unit_of_work_gets_the_user_for_update_and_then_gets_it_for_update_in_a_scope_of_its_own : after_one_unit_of_work_saves_a_user_and_a_vocab
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

   public class after_a_unit_of_work_that_saves_a_user_and_a_vocab_fails : Given_a_tevent_store_and_a_state_store
   {
      public after_a_unit_of_work_that_saves_a_user_and_a_vocab_fails() =>
         Invoking(() => InUnitOfWork(taggregates =>
         {
            SaveAUserAndAVocab(taggregates);
            throw new InvalidOperationException("The unit of work fails.");
         })).Must().Throw<InvalidOperationException>();

      [PCT] public void the_reader_gets_no_user() => Reader.TryGet(_userId, out User? _).Must().BeFalse();
      [PCT] public void the_reader_gets_no_vocab() => Reader.TryGet(_vocabId, out StoredVocab? _).Must().BeFalse();
   }
}
