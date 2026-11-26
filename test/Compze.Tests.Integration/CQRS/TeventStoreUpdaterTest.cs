using System.Collections.ObjectModel;
using System.Transactions;
using Compze.Abstractions;
using Compze.Abstractions.Time;
using Compze.xUnitMatrix;
using JetBrains.Annotations;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Internals.Testing;
using Compze.Tests.Common;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.SystemCE;
using Compze.Internals.SystemCE.TransactionsCE.Testing;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.Must;
using Compze.Tessaging.TessageBus;
using Compze.Teventive;
using Compze.Teventive.TaggregateStores;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Teventive.TeventStore.Abstractions;
using EnumerableCE = Compze.Internals.SystemCE.LinqCE.EnumerableCE;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Integration.CQRS;

public class TeventStoreUpdaterTest : UniversalTestBase
{
   class TeventSpy
   {
      public IEnumerable<ITaggregateTevent> DispatchedTessages => [.._tevents];
      public void Receive(ITaggregateTevent tevent) => _tevents.Add(tevent);
      readonly List<ITaggregateTevent> _tevents = [];
   }

   readonly TeventSpy _teventSpy;
   readonly IDependencyInjectionContainer _container;

   public TeventStoreUpdaterTest()
   {
      _teventSpy = new TeventSpy();

      //The user's tevents declare no delivery guarantee, so the spy subscribes synchronously to every taggregate tevent.
      _container = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireIntegrationTestTypeMappings(),
                                                             composeEngine: engine => engine.RegisterTessageBusHandlers(handle => handle.ForTevent((ITaggregateTevent tevent) => _teventSpy.Receive(tevent))),
                                                             declareTeventStoreTaggregates: store => store.Taggregate<User>());
   }

   protected override async Task DisposeAsyncInternal() => await _container.DisposeAsync().AsTask();

   protected void UseInTransactionalScope([InstantHandle] Action<ITaggregateStoreUpdater> useSession)
      => _container.ExecuteUnitOfWork(unitOfWork => useSession(unitOfWork.TaggregateStoreUpdater()));

   protected TResult UseInTransactionalScope<TResult>([InstantHandle] Func<ITaggregateStoreUpdater, TResult> useSession)
      => _container.ExecuteUnitOfWork(unitOfWork => useSession(unitOfWork.TaggregateStoreUpdater()));

   ITaggregateStoreReader Reader => _container.RootResolver.Resolve<ITaggregateStoreReader>();

   ITeventStoreReader TeventStoreReader => _container.RootResolver.Resolve<ITeventStoreReader>();

   [PCT]
   public void WhenFetchingTaggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown()
   {
      UseInTransactionalScope(session => MustActions.Invoking(() => session.GetForUpdate<User>(TaggregateId.New()))
                                                      .Must().Throw<ArgumentOutOfRangeException>());
   }

   [PCT]
   public void CanSaveAndLoadTaggregate()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());
      user.ChangePassword("NewPassword");
      user.ChangeEmail("NewEmail");

      UseInTransactionalScope(session => session.Add(user));

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.GetForUpdate<User>(user.Id);

         loadedUser.Id.Must().Be(user.Id);
         loadedUser.Email.Must().Be(user.Email);
         loadedUser.Password.Must().Be(user.Password);
      });
   }

   [PCT]
   public void CanLoadSpecificVersionOfTaggregate()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());
      user.ChangePassword("NewPassword");
      user.ChangeEmail("NewEmail");

      UseInTransactionalScope(session => session.Add(user));

      _container.ExecuteInIsolatedScope(scope =>
      {
         var reader = scope.TeventStoreReader();
         var loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 1);
         loadedUser.Id.Must().Be(user.Id);
         loadedUser.Email.Must().Be("email@email.se");
         loadedUser.Password.Must().Be("password");

         loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 2);
         loadedUser.Id.Must().Be(user.Id);
         loadedUser.Email.Must().Be("email@email.se");
         loadedUser.Password.Must().Be("NewPassword");

         loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 3);
         loadedUser.Id.Must().Be(user.Id);
         loadedUser.Email.Must().Be("NewEmail");
         loadedUser.Password.Must().Be("NewPassword");
      });
   }

   [PCT]
   public void ReturnsSameInstanceOnRepeatedLoads()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));

      UseInTransactionalScope(session =>
      {
         var loaded1 = session.GetForUpdate<User>(user.Id);
         var loaded2 = session.GetForUpdate<User>(user.Id);
         loaded1.Must().ReferenceEqual(loaded2);
      });
   }

   [PCT]
   public void ReturnsSameInstanceOnLoadAfterSave()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session =>
      {
         session.Add(user);

         var loaded1 = session.GetForUpdate<User>(user.Id);
         var loaded2 = session.GetForUpdate<User>(user.Id);
         loaded1.Must().ReferenceEqual(loaded2);
         loaded1.Must().ReferenceEqual(user);
      });
   }

   [PCT]
   public void TracksAndUpdatesLoadedTaggregates()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.GetForUpdate<User>(user.Id);
         loadedUser.ChangePassword("NewPassword");
      });

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.GetForUpdate<User>(user.Id);
         loadedUser.Password.Must().Be("NewPassword");
      });
   }

   [PCT]
   public void RefusesChangesToTaggregatesLoadedViaSpecificVersion()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));

      _container.ExecuteUnitOfWork(unitOfWork =>
      {
         var loadedUser = unitOfWork.TeventStoreReader().GetReadonlyCopyOfVersion<User>(user.Id, 1);
         MustActions.Invoking(() => loadedUser.ChangeEmail("NewEmail")).Must().Throw<InvalidOperationException>();
      });

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.GetForUpdate<User>(user.Id);
         loadedUser.Email.Must().Be("OriginalEmail");
      });
   }

   [PCT]
   public void ResetsTaggregatesAfterSaveChanges()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));
      ((ITaggregate)user).Commit(tevents => tevents.Must().BeEmpty());
   }

   [PCT]
   public void ThrowsWhenAttemptingToSaveExistingTaggregate()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));

      UseInTransactionalScope(session => MustActions.Invoking(() => session.Add(user))
                                                      .Must().Throw<InvalidOperationException>());
   }

   [PCT]
   public void DoesNotExplodeWhenSavingMoreThan10Tevents()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", TaggregateId.New());
      1.Through(100).ForEach(index => user.ChangeEmail("email" + index));

      UseInTransactionalScope(session => session.Add(user));
   }

   [PCT]
   public void TaggregateCannotBeRetrievedAfterBeingDeleted()
   {
      var user1 = new User();
      user1.Register("email1@email.se", "password", TaggregateId.New());

      var user2 = new User();
      user2.Register("email2@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session =>
      {
         session.Add(user1);
         session.Add(user2);
      });

      UseInTransactionalScope(session =>
      {
         session.GetForUpdate<User>(user1.Id).Delete();

         var loadedUser2 = session.GetForUpdate<User>(user2.Id);
         loadedUser2.Id.Must().Be(user2.Id);
         loadedUser2.Email.Must().Be(user2.Email);
         loadedUser2.Password.Must().Be(user2.Password);
      });

      UseInTransactionalScope(session => session.TryGetForUpdate(user1.Id, out User? _).Must().BeFalse());
   }

   [PCT]
   public void DeletingAnTaggregateDoesNotPreventTeventsFromItFromBeingRaised()
   {
      var user1 = new User();
      user1.Register("email1@email.se", "password", TaggregateId.New());

      var user2 = new User();
      user2.Register("email2@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session =>
      {
         session.Add(user1);
         session.Add(user2);
      });

      _teventSpy.DispatchedTessages.Count().Must().Be(2);

      UseInTransactionalScope(session =>
      {
         user1 = session.GetForUpdate<User>(user1.Id);

         user1.ChangeEmail("new_email");

         user1.Delete();
      });

      _teventSpy.DispatchedTessages.Skip(2).Select(it => it.GetType().Name).Must().SequenceEqual([nameof(UserChangedEmail), nameof(UserDeleted)]);
   }

   [PCT]
   public void Tevents_should_be_published_immediately()
   {
      UseInTransactionalScope(session =>
      {
         var user1 = new User();
         user1.Register("email1@email.se", "password", TaggregateId.New());
         session.Add(user1);

         _teventSpy.DispatchedTessages.Last()
                  .Must()
                  .BeExactType<UserRegistered>();

         user1 = session.GetForUpdate<User>(user1.Id);
         user1.ChangeEmail("new_email");
         _teventSpy.DispatchedTessages.Last()
                  .Must()
                  .BeExactType<UserChangedEmail>();
      });
   }

   [PCT]
   public void When_fetching_history_from_the_same_instance_after_updating_an_taggregate_the_fetched_history_includes_the_new_tevents()
   {
      var userId = TaggregateId.New();
      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Add(user);
      });

      UseInTransactionalScope(session =>
      {
         var user = session.GetForUpdate<User>(userId);
         user.ChangeEmail("new_email@email.com");
      });

      TeventStoreReader.GetHistory(userId).Count.Must().Be(2);
   }

   [PCT]
   public void When_a_taggregate_saved_in_one_unit_of_work_is_deleted_in_the_next_its_history_is_kept_ending_with_the_deleted_tevent_and_the_store_holds_no_such_taggregate()
   {
      var userId = TaggregateId.New();

      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Add(user);
      });

      UseInTransactionalScope(session => session.GetForUpdate<User>(userId).Delete());

      TeventStoreReader.GetHistory(userId).Select(it => it.Tevent.GetType().Name).Must().SequenceEqual([nameof(UserRegistered), nameof(UserDeleted)]);
      Reader.TryGet(userId, out User? _).Must().BeFalse();
   }

   [PCT]
   public void When_a_taggregate_is_saved_and_deleted_in_one_unit_of_work_its_history_is_kept_ending_with_the_deleted_tevent_and_the_store_holds_no_such_taggregate()
   {
      var userId = TaggregateId.New();

      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Add(user);
         user.Delete();
      });

      TeventStoreReader.GetHistory(userId).Select(it => it.Tevent.GetType().Name).Must().SequenceEqual([nameof(UserRegistered), nameof(UserDeleted)]);
      Reader.TryGet(userId, out User? _).Must().BeFalse();
   }

   [PCT]
   [Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "We have not been able to get this to work with SQLite, and since it is testing concurrency behavior is it somewhat outside of SQLite aims anyway...")]
   public async Task Concurrent_read_only_access_to_taggregate_history_can_occur_in_parallel()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));

      var withinTransactionAfterReadGate = IThreadGate.NewClosed(WaitTimeout.Seconds(30));
      await using var runner = new TestingTaskRunner(30.Seconds());

      runner.Start(ReadUserHistory, ReadUserHistory);
      (await withinTransactionAfterReadGate.TryAwaitQueueLengthEqualToAsync(2)).Must().BeTrue();
      withinTransactionAfterReadGate.Open();

      return;

      void ReadUserHistory() =>
         UseInTransactionalScope(_ =>
         {
            TeventStoreReader.GetHistory(user.Id);
            withinTransactionAfterReadGate.AwaitPassThrough();
         });
   }

   [PCT]
   public void TeventsArePublishedImmediatelyOnTaggregateChanges()
   {
      //Published with the time frozen, so that their time stamps have the microsecond precision that every supported database keeps.
      var users = UtcTimeSource.Test.FrozenAtUtcNow().Run(() => 1.Through(9).Select(i =>
      {
         var u = new User();
         u.Register(i + "@test.com", "abcd", TaggregateId.New());
         u.ChangeEmail("new" + i + "@test.com");
         return u;
      }).ToList());

      UseInTransactionalScope(session =>
      {
         users.Take(3).ForEach(session.Add);
         _teventSpy.DispatchedTessages.Count().Must().Be(6);
      });

      UseInTransactionalScope(session =>
      {
         _teventSpy.DispatchedTessages.Count().Must().Be(6);
         users.Skip(3).Take(3).ForEach(session.Add);
         _teventSpy.DispatchedTessages.Count().Must().Be(12);
      });

      UseInTransactionalScope(session =>
      {
         _teventSpy.DispatchedTessages.Count().Must().Be(12);
         users.Skip(6).Take(3).ForEach(session.Add);
         _teventSpy.DispatchedTessages.Count().Must().Be(18);
      });

      _container.ExecuteUnitOfWork(unitOfWork =>
      {
         _teventSpy.DispatchedTessages.Count().Must().Be(18);

         var dispatchedTevents = _teventSpy.DispatchedTessages.OfType<ITaggregateTeventWithIdentity>().ToList();
         dispatchedTevents.Select(e => e.Id).Distinct().Count().Must().Be(18);

         var allPersistedTevents = unitOfWork.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize().Tevents().ToList();

         allPersistedTevents.Must().DeepEqual(dispatchedTevents);
      });
   }

   [PCT]
   public async Task InsertNewTeventType_should_not_throw_exception_if_the_tevent_type_has_been_inserted_by_something_else()
   {
      var user = UseInTransactionalScope(session => User.Register(session, "email@email.se", "password", TaggregateId.New()));
      var otherUser = await ChangeAnotherUsersEmailInOtherInstance();

      UseInTransactionalScope(session => session.GetForUpdate<User>(otherUser.Id).Email.Must().Be("otheruser@email.new"));

      UseInTransactionalScope(_ => user.ChangeEmail("some@email.new"));
      return;

      async Task<User> ChangeAnotherUsersEmailInOtherInstance()
      {
         var clonedContainer = _container.CloneAndBuild();
         await using var container = clonedContainer;
         return clonedContainer.ExecuteUnitOfWork(unitOfWork =>
         {
            // ReSharper disable once AccessToDisposedClosure
            var session = unitOfWork.TaggregateStoreUpdater();
            var another = User.Register(session,
                                        "email@email.se",
                                        "password",
                                        TaggregateId.New());
            another.ChangeEmail("otheruser@email.new");
            return another;
         });
      }
   }

   [PCT]
   public void If_the_first_transaction_to_insert_an_tevent_of_specific_type_fails_the_next_succeeds()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());

      UseInTransactionalScope(session => session.Add(user));

      MustActions.Invoking(() => ChangeUserEmail(failOnPrepare: true))
                   .Must().Throw<Exception>();

      ChangeUserEmail(failOnPrepare: false);
      return;

      void ChangeUserEmail(bool failOnPrepare) =>
         UseInTransactionalScope(session =>
         {
            if(failOnPrepare)
            {
               Transaction.Current!.FailOnPrepare();
            }

            var loadedUser = session.GetForUpdate<User>(user.Id);
            loadedUser.ChangeEmail("new@email.com");
         });
   }

   [PCT]
   public async Task Serializes_access_to_an_taggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());
      UseInTransactionalScope(session =>
      {
         session.Add(user);
         user.ChangeEmail("newemail@somewhere.not");
      });

      var getHistorySection = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(30), "getHistorySection");
      var changeEmailSection = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(30), "changeEmailSection");

      const int threads = 2;
      var tasks = 1.Through(threads).Select(_ => TaskCE.RunOnDedicatedThreadAsync(UpdateEmail)).ToArray();

      await getHistorySection.LetOneThreadPassAsync();
      await changeEmailSection.LetOneThreadEnterAndReachExitAsync();
      changeEmailSection.Open();
      getHistorySection.Open();

      await Task.WhenAll(tasks); //Sql duplicate key (TaggregateId, Version) Exception would be thrown here if history was not serialized

      {
         var userHistory = TeventStoreReader.GetHistory(user.Id)
                                            .ToArray(); //Reading the taggregate will throw an exception if the history is invalid.
         userHistory.Length.Must()
                    .Be(threads + 2); //Make sure that all of the transactions completed
      }
      return;

      void UpdateEmail()
      {
         getHistorySection.Execute(() => TeventStoreReader.GetHistory(user.Id));

         UseInTransactionalScope(session =>
         {
            changeEmailSection.Execute(() =>
            {
               var userToUpdate = session.GetForUpdate<User>(user.Id);
               userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
            });
         });
      }
   }

   ///<summary>ITaggregateStoreUpdater.TryGetForUpdate promises that a unit of work that finds no taggregate holds its id's lock all the<br/>
   /// same, so that no other unit of work adds one before it completes: what makes a get-or-create safe.</summary>
   [PCT]
   [Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "SQLite serializes every write transaction, so it can't show a lock on one id.")]
   public async Task A_unit_of_work_that_finds_no_taggregate_to_get_for_update_keeps_another_from_getting_it_for_update_until_it_completes()
   {
      var userId = TaggregateId.New();
      var firstHasLookedForTheUser = IThreadGate.NewOpen(WaitTimeout.Seconds(30), "firstHasLookedForTheUser");
      var firstMayAddTheUser = IThreadGate.NewClosed(WaitTimeout.Seconds(30), "firstMayAddTheUser");
      var firstIsCompleting = false;
      var firstWasCompletingWhenTheSecondGotItsAnswer = false;
      var secondFoundTheUser = false;

      var first = TaskCE.RunOnDedicatedThreadAsync(() => UseInTransactionalScope(session =>
      {
         session.TryGetForUpdate(userId, out User? _).Must().BeFalse();
         firstHasLookedForTheUser.AwaitPassThrough();
         firstMayAddTheUser.AwaitPassThrough();
         User.Register(session, "added@example.com", "password", userId);
         Volatile.Write(ref firstIsCompleting, true);
      }));
      await firstHasLookedForTheUser.AwaitPassedThroughCountEqualToAsync(1);

      var second = TaskCE.RunOnDedicatedThreadAsync(() => UseInTransactionalScope(session =>
      {
         secondFoundTheUser = session.TryGetForUpdate(userId, out User? _);
         firstWasCompletingWhenTheSecondGotItsAnswer = Volatile.Read(ref firstIsCompleting);
      }));
      await Task.Delay(500.Milliseconds());
      firstMayAddTheUser.Open();
      await Task.WhenAll(first, second);

      firstWasCompletingWhenTheSecondGotItsAnswer.Must().BeTrue("the second unit of work should have waited for the first to complete");
      secondFoundTheUser.Must().BeTrue("the second unit of work should have found the user the first added");
   }

   [PCT]
   [Skip<SqlLayer>([SqlLayer.Sqlite, SqlLayer.SqliteMemory], "We have not been able to get this to work with SQLite, and since it is testing concurrency behavior is it somewhat outside of SQLite aims anyway...")]
   public async Task Serializes_access_to_an_taggregate_so_that_concurrent_transactions_succeed()
   {
      var user = new User();
      user.Register("email@email.se", "password", TaggregateId.New());
      UseInTransactionalScope(session =>
      {
         session.Add(user);
         user.ChangeEmail("newemail@somewhere.not");
      });

      var changeEmailSection = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(20), "changeEmailSection");
      var hasFetchedUser = IThreadGate.NewOpen(WaitTimeout.Seconds(20), "hasFetchedUser");

      const int threads = 2;

      var tasks = 1.Through(threads).Select(_ => TaskCE.RunOnDedicatedThreadAsync(UpdateEmail)).ToArray();

      changeEmailSection.EntranceGate.Open();
      await changeEmailSection.EntranceGate.AwaitPassedThroughCountEqualToAsync(2);
      await changeEmailSection.ExitGate.AwaitQueueLengthEqualToAsync(1);

      await Task.Delay(100.Milliseconds());

      var bothTasksReadUserException = ExceptionCE.TryCatch(() => hasFetchedUser.Passed.Must().Be(1, "Only one thread should have been able to fetch the taggregate"));

      var bothTasksCompletedException = ExceptionCE.TryCatch(() => changeEmailSection.ExitGate.Queued.Must().Be(1, "One thread should be blocked by transaction and never reach here until the other completes the transaction."));

      changeEmailSection.Open();

      var allTasks = Task.WhenAll(tasks);
      await allTasks.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
      var taskException = allTasks.Exception; //Sql duplicate key (TaggregateId, Version) Exception would be thrown here if history was not serialized. Or a deadlock will be thrown if the locking is not done correctly.

      if(bothTasksCompletedException != null || taskException != null || bothTasksReadUserException != null)
         throw new AggregateException(EnumerableCE.Create(bothTasksCompletedException).Append(bothTasksReadUserException).Concat(taskException?.InnerExceptions ?? new ReadOnlyCollection<Exception>([])).Where(it => it != null).Cast<Exception>());

      {
         var userHistory = TeventStoreReader.GetHistory(user.Id)
                                            .ToArray(); //Reading the taggregate will throw an exception if the history is invalid.
         userHistory.Length.Must()
                    .Be(threads + 2); //Make sure that all the transactions completed
      }
      return;

      void UpdateEmail() =>
         UseInTransactionalScope(session =>
         {
            changeEmailSection.Execute(() =>
            {
               var userToUpdate = session.GetForUpdate<User>(user.Id);
               hasFetchedUser.AwaitPassThrough();
               userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
            });
         });
   }
}
