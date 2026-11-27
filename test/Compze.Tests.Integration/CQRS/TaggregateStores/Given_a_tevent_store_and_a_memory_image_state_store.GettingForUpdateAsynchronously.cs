using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.Must;
using Compze.Teventive.TaggregateStores;
using Compze.Tests.Common;
using Compze.Tests.Common.CQRS.TeventRefactoring.Migrations;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Integration.CQRS.MemoryImageStateStore;
using static Compze.Must.MustActions;

#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Integration.CQRS.TaggregateStores;

///<summary>Getting for update asynchronously, <see cref="ITaggregateStoreUpdater.GetForUpdateAsync{TTaggregate}"/> and<br/>
/// <see cref="ITaggregateStoreUpdater.TryGetForUpdateAsync{TTaggregate}"/>, from both stores.</summary>
public partial class Given_a_tevent_store_and_a_memory_image_state_store
{
   async Task InUnitOfWorkAsync(Func<ITaggregateStoreUpdater, Task> work) =>
      await _container.ScopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork => await work(unitOfWork.TaggregateStoreUpdater()));

   ///<summary>Runs <paramref name="unitsOfWork"/> units of work at once, each started on a thread-pool thread, as an ASP.NET Core route's is:<br/>
   /// each does <paramref name="work"/> with its number.</summary>
   async Task AtOnceOnThreadPoolThreads(int unitsOfWork, Func<int, ITaggregateStoreUpdater, Task> work) =>
      await Task.WhenAll(Enumerable.Range(0, unitsOfWork).Select(unitOfWork => Task.Run(() => InUnitOfWorkAsync(taggregates => work(unitOfWork, taggregates)))));

   static async Task<string> MessageOfTheExceptionThrownByAsync<TException>(Func<Task> work) where TException : Exception =>
      (await InvokingAsync(work).Must().ThrowAsync<TException>()).Which.Message;

   public partial class after_one_unit_of_work_adds_a_user_and_a_vocab
   {
      [PCT] public async Task getting_the_user_for_update_asynchronously_as_another_class_its_store_keeps_throws_naming_the_user_and_both_classes() =>
         (await MessageOfTheExceptionThrownByAsync<InvalidOperationException>(() => InUnitOfWorkAsync(async taggregates => await taggregates.GetForUpdateAsync<TestTaggregate>(_userId))))
        .Must().Satisfy(it => NamesTheTaggregateAndBothClasses(it, _userId, typeof(User), typeof(TestTaggregate)));

      [PCT] public async Task getting_the_vocab_for_update_asynchronously_as_another_class_its_store_keeps_throws_naming_the_vocab_and_both_classes() =>
         (await MessageOfTheExceptionThrownByAsync<InvalidOperationException>(() => InUnitOfWorkAsync(async taggregates => await taggregates.GetForUpdateAsync<StoredSentence>(_vocabId))))
        .Must().Satisfy(it => NamesTheTaggregateAndBothClasses(it, _vocabId, typeof(StoredVocab), typeof(StoredSentence)));

      public class and_a_second_unit_of_work_gets_both_for_update_asynchronously_and_changes_them_after_an_await : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         string _emailASecondGetForUpdateGave = "";
         string _meaningASecondGetForUpdateGave = "";

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await InUnitOfWorkAsync(async taggregates =>
            {
               var user = await taggregates.GetForUpdateAsync<User>(_userId);
               var vocab = await taggregates.GetForUpdateAsync<StoredVocab>(_vocabId);
               await Task.Yield();
               user.ChangeEmail("changed@example.com");
               vocab.ChangeMeaning("to eat");
               _emailASecondGetForUpdateGave = (await taggregates.GetForUpdateAsync<User>(_userId)).Email;
               _meaningASecondGetForUpdateGave = (await taggregates.GetForUpdateAsync<StoredVocab>(_vocabId)).Meaning;
            });
         }

         [PCT] public void a_second_get_for_update_of_the_user_in_the_unit_of_work_gave_its_changed_email() => _emailASecondGetForUpdateGave.Must().Be("changed@example.com");
         [PCT] public void a_second_get_for_update_of_the_vocab_in_the_unit_of_work_gave_its_changed_meaning() => _meaningASecondGetForUpdateGave.Must().Be("to eat");
         [PCT] public void the_reader_gets_the_users_changed_email() => Reader.Get<User>(_userId).Email.Must().Be("changed@example.com");
         [PCT] public void the_reader_gets_the_vocabs_changed_meaning() => Reader.Get<StoredVocab>(_vocabId).Meaning.Must().Be("to eat");
      }

      public class and_a_second_unit_of_work_tries_to_get_for_update_asynchronously_a_user_and_a_vocab_no_store_holds : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         readonly TaggregateId _idOfNoUser = TaggregateId.New();
         readonly TaggregateId _idOfNoVocab = new(Guid.NewGuid());
         User? _userTriedFor;
         StoredVocab? _vocabTriedFor;

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await InUnitOfWorkAsync(async taggregates =>
            {
               _userTriedFor = await taggregates.TryGetForUpdateAsync<User>(_idOfNoUser);
               _vocabTriedFor = await taggregates.TryGetForUpdateAsync<StoredVocab>(_idOfNoVocab);
            });
         }

         [PCT] public void trying_for_the_user_gave_none() => _userTriedFor.Must().BeNull();
         [PCT] public void trying_for_the_vocab_gave_none() => _vocabTriedFor.Must().BeNull();

         [PCT] public async Task getting_the_user_for_update_asynchronously_throws_naming_its_id() =>
            (await MessageOfTheExceptionThrownByAsync<ArgumentOutOfRangeException>(() => InUnitOfWorkAsync(async taggregates => await taggregates.GetForUpdateAsync<User>(_idOfNoUser))))
           .Must().Contain(_idOfNoUser.ToString());

         [PCT] public async Task getting_the_vocab_for_update_asynchronously_throws_naming_its_id() =>
            (await MessageOfTheExceptionThrownByAsync<ArgumentOutOfRangeException>(() => InUnitOfWorkAsync(async taggregates => await taggregates.GetForUpdateAsync<StoredVocab>(_idOfNoVocab))))
           .Must().Contain(_idOfNoVocab.ToString());
      }

      ///<summary>Each unit of work starts on a thread-pool thread, as an ASP.NET Core route's does, four times as many at once as the pool<br/>
      /// keeps threads at least. Each gets the deck for update asynchronously, awaits while it holds it, and adds a card. The others wait<br/>
      /// for the deck holding no thread, so each unit of work holding it finds a thread to resume on. Waiting through<br/>
      /// <see cref="ITaggregateStoreUpdater.GetForUpdate{TTaggregate}"/> instead, each would hold a pool thread, and the units of work<br/>
      /// would time out: see dev_docs/async-local-navigators.md.</summary>
      public class and_four_units_of_work_for_each_processor_at_once_each_get_a_deck_for_update_asynchronously_and_add_a_card_after_an_await : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         static readonly int UnitsOfWork = Environment.ProcessorCount * 4;
         readonly TaggregateId _deckId = new(Guid.NewGuid());

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            InUnitOfWork(taggregates => taggregates.Add(StoredDeck.Create(_deckId)));
            await AtOnceOnThreadPoolThreads(UnitsOfWork, async (unitOfWork, taggregates) =>
            {
               var deck = await taggregates.GetForUpdateAsync<StoredDeck>(_deckId);
               await Task.Delay(TimeSpan.FromMilliseconds(10));
               deck.AddCard($"card {unitOfWork}");
            });
         }

         [PCT] public void the_deck_has_a_card_from_each_unit_of_work() => Reader.Get<StoredDeck>(_deckId).Cards.InCreationOrder.Count.Must().Be(UnitsOfWork);
      }

      ///<summary>As for the deck above, with the user, which the tevent store keeps: eight units of work, more than a CI runner's pool<br/>
      /// keeps threads at least, and fewer than a test database's pool has connections. Each waiting for the user holds a connection of<br/>
      /// the pool, so more than the pool has connections are <see cref="TeventStore.Given_a_tevent_store_holding_a_user"/>'s.</summary>
      public class and_eight_units_of_work_at_once_each_get_the_user_for_update_asynchronously_and_change_its_email_after_an_await : after_one_unit_of_work_adds_a_user_and_a_vocab
      {
         const int UnitsOfWork = 8;

         protected override async Task InitializeAsyncInternal()
         {
            await base.InitializeAsyncInternal();
            await AtOnceOnThreadPoolThreads(UnitsOfWork, async (unitOfWork, taggregates) =>
            {
               var user = await taggregates.GetForUpdateAsync<User>(_userId);
               await Task.Delay(TimeSpan.FromMilliseconds(10));
               user.ChangeEmail($"user{unitOfWork}@example.com");
            });
         }

         [PCT] public void the_users_history_has_an_email_change_from_each_unit_of_work() => TeventStoreReader.GetHistory(_userId).Count(it => it.Tevent is IUserChangedEmail).Must().Be(UnitsOfWork);
      }
   }
}
