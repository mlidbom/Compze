using System.Text.Json;
using System.Transactions;
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

///<summary>A tevent store keeping users and a state store keeping vocab in files, in one container. A unit of work that writes the files<br/>
/// can't also hold the tevent store's database connection, unless the files allow it: a crash between the two commits would lose one of<br/>
/// them.</summary>
public class Given_a_tevent_store_and_a_state_store_kept_in_files : UniversalTestBase
{
   static readonly JsonSerializerOptions Options = new() { Converters = { new TaggregateIdAsAGuid() } };
   readonly DirectoryInfo _folder = new(Path.Combine(Path.GetTempPath(), "compze-tevent-store-and-state-store-in-files", Guid.NewGuid().ToString("N")));
   readonly TaggregateId _userId = new();
   readonly TaggregateId _vocabId = new(Guid.NewGuid());
   bool _filesAreAllowedInTransactionsThatAlsoWriteADatabase;
   IDependencyInjectionContainer? _container;

   IDependencyInjectionContainer Container => _container ??= TestEnv.DIContainer.SetupTestingContainer(
                                                 registrar => registrar.RequireIntegrationTestTypeMappings(),
                                                 configureContainer: register => register.StateStore(store => store.Taggregate(() => new StoredVocab())
                                                                                                                    .StoredInFiles(_folder, Options, DeclareFiles)),
                                                 declareTeventStoreTaggregates: store => store.Taggregate<User>());

   void DeclareFiles(TaggregateFiles files)
   {
      files.Of<StoredVocab>("vocab").KeptInMemory();
      if(_filesAreAllowedInTransactionsThatAlsoWriteADatabase) files.AllowedInTransactionsThatAlsoWriteADatabase();
   }

   protected override async Task DisposeAsyncInternal()
   {
      if(_container != null) await _container.DisposeAsync();
   }

   ITaggregateStoreReader Reader => Container.RootResolver.Resolve<ITaggregateStoreReader>();

   ITaggregateFilesInMemory Files => Container.RootResolver.Resolve<ITaggregateFilesInMemory>();

   ///<summary>The vocab's file in the default layout: &lt;first two hex digits of the id&gt;/&lt;id&gt;.json under the vocab folder.</summary>
   string VocabFile => Path.Combine(_folder.FullName, "vocab", _vocabId.Value.ToString("N")[..2], $"{_vocabId.Value}.json");

   string StoresOwnFolder => Path.Combine(_folder.FullName, ".state-store");

   IReadOnlyList<string> FilesInTheStoresOwnFolder => [..Files.Paths.Where(it => it.StartsWith(StoresOwnFolder, StringComparison.OrdinalIgnoreCase))];

   void InUnitOfWork(Action<ITaggregateStoreUpdater> work) => Container.ExecuteUnitOfWork(unitOfWork => work(unitOfWork.TaggregateStoreUpdater()));

   void SaveAUserAndAVocabInUnitsOfWorkOfTheirOwn()
   {
      InUnitOfWork(taggregates => User.Register(taggregates, "registered@example.com", "password", _userId));
      InUnitOfWork(taggregates => taggregates.Save(StoredVocab.Create(_vocabId, "taberu")));
   }

   static string SerializedVocab(TaggregateId id, string question, string meaning) => JsonSerializer.Serialize(new StoredVocabSnapshot(id, question, meaning), Options);

   public class after_a_user_and_a_vocab_are_saved_in_units_of_work_of_their_own : Given_a_tevent_store_and_a_state_store_kept_in_files
   {
      public after_a_user_and_a_vocab_are_saved_in_units_of_work_of_their_own() => SaveAUserAndAVocabInUnitsOfWorkOfTheirOwn();

      [PCT] public void the_reader_gets_the_user() => Reader.Get<User>(_userId).Email.Must().Be("registered@example.com");

      [PCT] public void the_reader_gets_the_vocab() => Reader.Get<StoredVocab>(_vocabId).Question.Must().Be("taberu");

      public class and_a_unit_of_work_gets_the_user_for_update_and_changes_the_vocabs_meaning : after_a_user_and_a_vocab_are_saved_in_units_of_work_of_their_own
      {
         readonly Exception _exception;

         public and_a_unit_of_work_gets_the_user_for_update_and_changes_the_vocabs_meaning() =>
            _exception = Invoking(() => InUnitOfWork(taggregates =>
                         {
                            taggregates.GetForUpdate<User>(_userId);
                            taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaning("to eat");
                         }))
                        .Must().Throw<TransactionAbortedException>().Which;

         [PCT] public void the_unit_of_work_is_refused_naming_the_state_stores_folder_and_the_database_connection() =>
            _exception.InnerException!.Message.Must().Satisfy(it => it.Contains(_folder.FullName, StringComparison.Ordinal) && it.Contains("a database connection", StringComparison.Ordinal));

         [PCT] public void the_refusal_names_the_declaration_that_allows_it() =>
            _exception.InnerException!.Message.Must().Contain(nameof(TaggregateFiles.AllowedInTransactionsThatAlsoWriteADatabase));

         [PCT] public void the_reader_gets_the_vocabs_meaning_committed_before() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("");

         [PCT] public void the_vocabs_file_still_holds_the_meaning_committed_before() => Files.ReadAllText(VocabFile).Must().Be(SerializedVocab(_vocabId, "taberu", ""));

         [PCT] public void the_stores_own_folder_holds_no_temporary_file() => FilesInTheStoresOwnFolder.Must().BeEmpty();
      }

      ///<summary>The tevent store writes each tevent as it is published, so its connection is in a unit of work's transaction before the<br/>
      /// files join it at the prepare. A participant that opens a connection of Compze's at its own prepare joins after them.</summary>
      public class and_a_unit_of_work_changes_the_vocabs_meaning_and_then_a_connection_to_the_tevent_store_joins_at_its_prepare : after_a_user_and_a_vocab_are_saved_in_units_of_work_of_their_own
      {
         readonly Exception _exception;

         public and_a_unit_of_work_changes_the_vocabs_meaning_and_then_a_connection_to_the_tevent_store_joins_at_its_prepare() =>
            _exception = Invoking(() => InUnitOfWork(taggregates =>
                         {
                            taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaning("to eat");
                            var transaction = Transaction.Current!;
                            transaction.EnlistVolatile(new ParticipantThatReadsAHistoryAtItsPrepare(transaction, () => Container.RootResolver.Resolve<ITeventStoreReader>().GetHistory(_userId)),
                                                       EnlistmentOptions.EnlistDuringPrepareRequired);
                         }))
                        .Must().Throw<TransactionAbortedException>().Which;

         [PCT] public void the_unit_of_work_is_refused_naming_the_state_stores_folder_and_the_database_connection() =>
            _exception.InnerException!.Message.Must().Satisfy(it => it.Contains(_folder.FullName, StringComparison.Ordinal) && it.Contains("a database connection", StringComparison.Ordinal));

         [PCT] public void the_reader_gets_the_vocabs_meaning_committed_before() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("");
      }

      public class and_a_unit_of_work_gets_the_vocab_for_update_without_changing_it_and_changes_the_users_email : after_a_user_and_a_vocab_are_saved_in_units_of_work_of_their_own
      {
         public and_a_unit_of_work_gets_the_vocab_for_update_without_changing_it_and_changes_the_users_email() =>
            InUnitOfWork(taggregates =>
            {
               taggregates.GetForUpdate<StoredVocab>(_vocabId);
               taggregates.GetForUpdate<User>(_userId).ChangeEmail("changed@example.com");
            });

         [PCT] public void the_reader_gets_the_users_new_email() => Reader.Get<User>(_userId).Email.Must().Be("changed@example.com");
      }
   }

   public class whose_files_are_allowed_in_transactions_that_also_write_a_database : Given_a_tevent_store_and_a_state_store_kept_in_files
   {
      protected whose_files_are_allowed_in_transactions_that_also_write_a_database() => _filesAreAllowedInTransactionsThatAlsoWriteADatabase = true;

      public class after_a_unit_of_work_saves_a_user_and_a_vocab_and_another_changes_both : whose_files_are_allowed_in_transactions_that_also_write_a_database
      {
         public after_a_unit_of_work_saves_a_user_and_a_vocab_and_another_changes_both()
         {
            InUnitOfWork(taggregates =>
            {
               User.Register(taggregates, "registered@example.com", "password", _userId);
               taggregates.Save(StoredVocab.Create(_vocabId, "taberu"));
            });
            InUnitOfWork(taggregates =>
            {
               taggregates.GetForUpdate<User>(_userId).ChangeEmail("changed@example.com");
               taggregates.GetForUpdate<StoredVocab>(_vocabId).ChangeMeaning("to eat");
            });
         }

         [PCT] public void the_reader_gets_the_users_new_email() => Reader.Get<User>(_userId).Email.Must().Be("changed@example.com");

         [PCT] public void the_reader_gets_the_vocabs_new_meaning() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("to eat");

         [PCT] public void the_vocabs_file_holds_the_new_meaning() => Files.ReadAllText(VocabFile).Must().Be(SerializedVocab(_vocabId, "taberu", "to eat"));

         [PCT] public void the_stores_own_folder_holds_no_temporary_file_or_journal() => FilesInTheStoresOwnFolder.Must().BeEmpty();
      }
   }

   ///<summary>A participant that, when the transaction prepares, reads with <paramref name="read"/> inside the transaction, as code<br/>
   /// writing to a database at the prepare does, and rolls the transaction back with whatever that throws.</summary>
   sealed class ParticipantThatReadsAHistoryAtItsPrepare(Transaction transaction, Action read) : IEnlistmentNotification
   {
      readonly Transaction _transaction = transaction;
      readonly Action _read = read;

      public void Prepare(PreparingEnlistment preparingEnlistment)
      {
         try
         {
            using var scope = new TransactionScope(_transaction);
            _read();
            scope.Complete();
         }
#pragma warning disable CA1031 //A participant rolls the transaction back with what its prepare throws.
         catch(Exception exception)
#pragma warning restore CA1031
         {
            preparingEnlistment.ForceRollback(exception);
            return;
         }
         preparingEnlistment.Prepared();
      }

      public void Commit(Enlistment enlistment) => enlistment.Done();
      public void Rollback(Enlistment enlistment) => enlistment.Done();
      public void InDoubt(Enlistment enlistment) => enlistment.Done();
   }
}
