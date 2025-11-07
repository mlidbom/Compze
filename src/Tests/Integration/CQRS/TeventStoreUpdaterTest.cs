using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Core.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.Core.Wiring.Testing.Internal;
using JetBrains.Annotations;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.SystemCE;
using Compze.Utilities.SystemCE.TransactionsCE.Testing;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.ThreadingCE.Testing;
using Compze.Utilities.Testing.Must;
using EnumerableCE = Compze.Utilities.SystemCE.LinqCE.EnumerableCE;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Integration.CQRS;

public class TeventStoreUpdaterTest : UniversalTestBase
{
   class TeventSpy
   {
      public IEnumerable<IExactlyOnceTevent> DispatchedTessages => _tevents.ToList();
      public void Receive(IExactlyOnceTevent tevent) => _tevents.Add(tevent);
      readonly List<IExactlyOnceTevent> _tevents = [];
   }

   readonly TeventSpy _teventSpy;
   readonly IServiceLocator _serviceLocator;

   public TeventStoreUpdaterTest()
   {
      _serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator(null);

      _teventSpy = new TeventSpy();

      _serviceLocator.Resolve<ITessageHandlerRegistrar>()
                     .ForTevent<IExactlyOnceTevent>(_teventSpy.Receive);
   }

   protected override async Task DisposeAsyncInternal() => await _serviceLocator.DisposeAsync().AsTask();

   protected void UseInTransactionalScope([InstantHandle] Action<ITeventStoreUpdater> useSession)
      => _serviceLocator.ExecuteTransactionInIsolatedScope(() => useSession(_serviceLocator.Resolve<ITeventStoreUpdater>()));

   protected TResult UseInTransactionalScope<TResult>([InstantHandle] Func<ITeventStoreUpdater, TResult> useSession)
      => _serviceLocator.ExecuteTransactionInIsolatedScope(() => useSession(_serviceLocator.Resolve<ITeventStoreUpdater>()));

   public void UseInScope([InstantHandle] Action<ITeventStoreUpdater> useSession)
      => _serviceLocator.ExecuteInIsolatedScope(() => useSession(_serviceLocator.Resolve<ITeventStoreUpdater>()));

   [PCT]
   public void WhenFetchingTaggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown()
   {
      UseInTransactionalScope(session => MustActions.Invoking(() => session.Get<User>(new TaggregateId()))
                                                      .Must().Throw<ArgumentOutOfRangeException>());
   }

   [PCT]
   public void CanSaveAndLoadTaggregate()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());
      user.ChangePassword("NewPassword");
      user.ChangeEmail("NewEmail");

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);

         loadedUser.Id.Must().Be(user.Id);
         loadedUser.Email.Must().Be(user.Email);
         loadedUser.Password.Must().Be(user.Password);
      });
   }

   [PCT]
   public void ThrowsIfUsedByMultipleThreads()
   {
      ITeventStoreUpdater? updater = null;
      ITeventStoreReader? reader = null;
      using var wait = new ManualResetEventSlim();
      TaskCE.Run(() =>
      {
         _serviceLocator.ExecuteInIsolatedScope(() =>
         {
            updater = _serviceLocator.Resolve<ITeventStoreUpdater>();
            reader = _serviceLocator.Resolve<ITeventStoreReader>();
         });
         wait.Set();
      });
      wait.Wait();
      updater = updater.NotNull();
      reader = reader.NotNull();

      MustActions.Invoking(() => updater.Get<User>(new TaggregateId())).Must().Throw<MultiThreadedUseException>();
      MustActions.Invoking(() => updater.Dispose()).Must().Throw<MultiThreadedUseException>();
      MustActions.Invoking(() => reader.GetReadonlyCopyOfVersion<User>(new TaggregateId(), 1)).Must().Throw<MultiThreadedUseException>();
      MustActions.Invoking(() => updater.Save(new User())).Must().Throw<MultiThreadedUseException>();
      MustActions.Invoking(() => updater.TryGet(new TaggregateId(), out User? _)).Must().Throw<MultiThreadedUseException>();
   }

   [PCT]
   public void CanLoadSpecificVersionOfTaggregate()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());
      user.ChangePassword("NewPassword");
      user.ChangeEmail("NewEmail");

      UseInTransactionalScope(session => session.Save(user));

      UseInScope(_ =>
      {
         var reader = _serviceLocator.Resolve<ITeventStoreReader>();
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
      user.Register("email@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loaded1 = session.Get<User>(user.Id);
         var loaded2 = session.Get<User>(user.Id);
         loaded1.Must().ReferenceEqual(loaded2);
      });
   }

   [PCT]
   public void ReturnsSameInstanceOnLoadAfterSave()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session =>
      {
         session.Save(user);

         var loaded1 = session.Get<User>(user.Id);
         var loaded2 = session.Get<User>(user.Id);
         loaded1.Must().ReferenceEqual(loaded2);
         loaded1.Must().ReferenceEqual(user);
      });
   }

   [PCT]
   public void TracksAndUpdatesLoadedTaggregates()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);
         loadedUser.ChangePassword("NewPassword");
      });

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);
         loadedUser.Password.Must().Be("NewPassword");
      });
   }

   [PCT]
   public void DoesNotUpdateTaggregatesLoadedViaSpecificVersion()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(_ =>
      {
         var loadedUser = _serviceLocator.Resolve<ITeventStoreReader>().GetReadonlyCopyOfVersion<User>(user.Id, 1);
         loadedUser.ChangeEmail("NewEmail");
      });

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);
         loadedUser.Email.Must().Be("OriginalEmail");
      });
   }

   [PCT]
   public void ResetsTaggregatesAfterSaveChanges()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));
      ((ITaggregate)user).Commit(tevents => tevents.Must().BeEmpty());
   }

   [PCT]
   public void ThrowsWhenAttemptingToSaveExistingTaggregate()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session => MustActions.Invoking(() => session.Save(user))
                                                      .Must().Throw<InvalidOperationException>());
   }

   [PCT]
   public void DoesNotExplodeWhenSavingMoreThan10Tevents()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", new TaggregateId());
      1.Through(100).ForEach(index => user.ChangeEmail("email" + index));

      UseInTransactionalScope(session => session.Save(user));
   }

   [PCT]
   public void TaggregateCannotBeRetrievedAfterBeingDeleted()
   {
      var user1 = new User();
      user1.Register("email1@email.se", "password", new TaggregateId());

      var user2 = new User();
      user2.Register("email2@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session =>
      {
         session.Save(user1);
         session.Save(user2);
      });

      UseInTransactionalScope(session =>
      {
         session.Delete(user1.Id);

         var loadedUser2 = session.Get<User>(user2.Id);
         loadedUser2.Id.Must().Be(user2.Id);
         loadedUser2.Email.Must().Be(user2.Email);
         loadedUser2.Password.Must().Be(user2.Password);
      });

      UseInTransactionalScope(session => session.TryGet(user1.Id, out User? _).Must().BeFalse());
   }

   [PCT]
   public void DeletingAnTaggregateDoesNotPreventTeventsFromItFromBeingRaised()
   {
      var user1 = new User();
      user1.Register("email1@email.se", "password", new TaggregateId());

      var user2 = new User();
      user2.Register("email2@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session =>
      {
         session.Save(user1);
         session.Save(user2);
      });

      _teventSpy.DispatchedTessages.Count().Must().Be(2);

      UseInTransactionalScope(session =>
      {
         user1 = session.Get<User>(user1.Id);

         user1.ChangeEmail("new_email");

         session.Delete(user1.Id);
      });

      var published = _teventSpy.DispatchedTessages.ToList();
      _teventSpy.DispatchedTessages.Count()
               .Must()
               .Be(3);
      published.Last().Must().BeExactType<UserChangedEmail>();
   }

   [PCT]
   public void Tevents_should_be_published_immediately()
   {
      UseInTransactionalScope(session =>
      {
         var user1 = new User();
         user1.Register("email1@email.se", "password", new TaggregateId());
         session.Save(user1);

         _teventSpy.DispatchedTessages.Last()
                  .Must()
                  .BeExactType<UserRegistered>();

         user1 = session.Get<User>(user1.Id);
         user1.ChangeEmail("new_email");
         _teventSpy.DispatchedTessages.Last()
                  .Must()
                  .BeExactType<UserChangedEmail>();
      });
   }

   [PCT]
   public void When_fetching_history_from_the_same_instance_after_updating_an_taggregate_the_fetched_history_includes_the_new_tevents()
   {
      var userId = new TaggregateId();
      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Save(user);
      });

      UseInTransactionalScope(session =>
      {
         var user = session.Get<User>(userId);
         user.ChangeEmail("new_email@email.com");
      });

      UseInScope(session =>
      {
         var history = ((ITeventStoreReader)session).GetHistory(userId);
         history.Count.Must().Be(2);
      });
   }

   [PCT]
   public void When_deleting_and_then_fetching_an_taggregates_history_the_history_should_be_gone()
   {
      var userId = new TaggregateId();

      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Save(user);
      });

      UseInTransactionalScope(session => session.Delete(userId));

      UseInScope(session =>
      {
         var history = ((ITeventStoreReader)session).GetHistory(userId);
         history.Count.Must().Be(0);
      });
   }

   [PCT]
   public void When_fetching_and_deleting_an_taggregate_then_fetching_history_again_the_history_should_be_gone()
   {
      var userId = new TaggregateId();

      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Save(user);
      });

      UseInTransactionalScope(session =>
      {
         session.Get<User>(userId);
         session.Delete(userId);
      });

      UseInScope(session =>
      {
         var history = ((ITeventStoreReader)session).GetHistory(userId);
         history.Count.Must().Be(0);
      });
   }

   [PCT(Skipped = [SqlLayer.Sqlite, SqlLayer.SqliteMemory], 
        SkipReasons = ["Sqlite is not really designed for high concurrency, we have not been able to get this working with SQLite",
                       "Sqlite is not really designed for high concurrency, we have not been able to get this working with SQLite"])]
   public void Concurrent_read_only_access_to_taggregate_history_can_occur_in_parallel()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));

      const int threadedIterations = 20;
      var delayEachTransactionBy = 1.Milliseconds();

      var singleThreadedExecutionTime = StopwatchCE.TimeExecution(ReadUserHistory, iterations: threadedIterations).Total;

      var timingsSummary = TimeAsserter.ExecuteThreaded(
         action: ReadUserHistory,
         iterations: threadedIterations,
         maxTotal: singleThreadedExecutionTime / 2,
         maxDegreeOfParallelism: 5,
         description: $"If access is serialized the time will be approximately {singleThreadedExecutionTime} milliseconds. If parallelized it should be far below this value.");

      timingsSummary.IndividualExecutionTimes.Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2).Must().BeGreaterThan(timingsSummary.Total, "If the sum elapsed time of the parts that run in parallel is not greater than the clock time passed parallelism is not taking place.");
      return;

      void ReadUserHistory() =>
         UseInTransactionalScope(session =>
         {
            ((ITeventStoreReader)session).GetHistory(user.Id);
            Thread.Sleep(delayEachTransactionBy);
         });
   }

   [PCT]
   public void TeventsArePublishedImmediatelyOnTaggregateChanges()
   {
      var users = 1.Through(9).Select(i =>
      {
         var u = new User();
         u.Register(i + "@test.com", "abcd", new TaggregateId());
         u.ChangeEmail("new" + i + "@test.com");
         return u;
      }).ToList();

      UseInTransactionalScope(session =>
      {
         users.Take(3).ForEach(session.Save);
         _teventSpy.DispatchedTessages.Count().Must().Be(6);
      });

      UseInTransactionalScope(session =>
      {
         _teventSpy.DispatchedTessages.Count().Must().Be(6);
         users.Skip(3).Take(3).ForEach(session.Save);
         _teventSpy.DispatchedTessages.Count().Must().Be(12);
      });

      UseInTransactionalScope(session =>
      {
         _teventSpy.DispatchedTessages.Count().Must().Be(12);
         users.Skip(6).Take(3).ForEach(session.Save);
         _teventSpy.DispatchedTessages.Count().Must().Be(18);
      });

      UseInTransactionalScope(_ =>
      {
         _teventSpy.DispatchedTessages.Count().Must().Be(18);

         var dispatchedTevents = _teventSpy.DispatchedTessages.OfType<ITaggregateTevent>().ToList();
         dispatchedTevents.Select(e => e.Id).Distinct().Count().Must().Be(18);

         var allPersistedTevents = _serviceLocator.TeventStore().ListAllTeventsForTestingPurposesAbsolutelyNotUsableForARealTeventStoreOfAnySize();
         TeventStorageTestHelper.StripSteventhDecimalPointFromSecondFractionOnUtcUpdateTime(dispatchedTevents);
         TeventStorageTestHelper.StripSteventhDecimalPointFromSecondFractionOnUtcUpdateTime(allPersistedTevents);

         allPersistedTevents.Must().DeepEqual(dispatchedTevents);
      });
   }

   [PCT]
   public async Task InsertNewTeventType_should_not_throw_exception_if_the_tevent_type_has_been_inserted_by_something_else()
   {
      var user = UseInTransactionalScope(session => User.Register(session, "email@email.se", "password", new TaggregateId()));
      var otherUser = await ChangeAnotherUsersEmailInOtherInstance();

      UseInTransactionalScope(session => session.Get<User>(otherUser.Id).Email.Must().Be("otheruser@email.new"));

      UseInTransactionalScope(_ => user.ChangeEmail("some@email.new"));
      return;

      async Task<User> ChangeAnotherUsersEmailInOtherInstance()
      {
         var clonedServiceLocator = _serviceLocator.Clone();
         await using var serviceLocator = clonedServiceLocator;
         return clonedServiceLocator.ExecuteTransactionInIsolatedScope(() =>
         {
            // ReSharper disable once AccessToDisposedClosure
            var session = clonedServiceLocator.Resolve<ITeventStoreUpdater>();
            var another = User.Register(session,
                                        "email@email.se",
                                        "password",
                                        new TaggregateId());
            another.ChangeEmail("otheruser@email.new");
            return another;
         });
      }
   }

   [PCT]
   public void If_the_first_transaction_to_insert_an_tevent_of_specific_type_fails_the_next_succeeds()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());

      UseInTransactionalScope(session => session.Save(user));

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

            var loadedUser = session.Get<User>(user.Id);
            loadedUser.ChangeEmail("new@email.com");
         });
   }

   [PCT]
   public void Serializes_access_to_an_taggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());
      UseInTransactionalScope(session =>
      {
         session.Save(user);
         user.ChangeEmail("newemail@somewhere.not");
      });

      var getHistorySection = GatedCodeSection.WithTimeout(2.Seconds());
      var changeEmailSection = GatedCodeSection.WithTimeout(2.Seconds());

      const int threads = 2;
      var tasks = 1.Through(threads).Select(_ => TaskCE.Run(UpdateEmail)).ToArray();

      getHistorySection.LetOneThreadPass();
      changeEmailSection.LetOneThreadEnterAndReachExit();
      changeEmailSection.Open();
      getHistorySection.Open();

      Task.WaitAll(tasks); //Sql duplicate key (TaggregateId, Version) Exception would be thrown here if history was not serialized

      UseInScope(session =>
      {
         var userHistory = ((ITeventStoreReader)session).GetHistory(user.Id)
                                                       .ToArray(); //Reading the taggregate will throw an exception if the history is invalid.
         userHistory.Length.Must()
                    .Be(threads + 2); //Make sure that all of the transactions completed
      });
      return;

      void UpdateEmail() =>
         UseInScope(session =>
         {
            using(getHistorySection.Enter())
            {
               ((ITeventStoreReader)session).GetHistory(user.Id);
            }

            TransactionScopeCe.Execute(() =>
            {
               using(changeEmailSection.Enter())
               {
                  var userToUpdate = session.Get<User>(user.Id);
                  userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
               }
            });
         });
   }

   [PCT(Skipped = [SqlLayer.Sqlite, SqlLayer.SqliteMemory],
        SkipReasons = ["We have not been able to get this to work with SQLite, and since it is testing concurrency behavior is it somewhat outside of SQLite aims anyway...",
                       "We have not been able to get this to work with SQLite, and since it is testing concurrency behavior is it somewhat outside of SQLite aims anyway..."])]
   public void Serializes_access_to_an_taggregate_so_that_concurrent_transactions_succeed()
   {
      var user = new User();
      user.Register("email@email.se", "password", new TaggregateId());
      UseInTransactionalScope(session =>
      {
         session.Save(user);
         user.ChangeEmail("newemail@somewhere.not");
      });

      var changeEmailSection = GatedCodeSection.WithTimeout(20.Seconds());
      var hasFetchedUser = ThreadGate.CreateOpenWithTimeout(20.Seconds());

      const int threads = 2;

      var tasks = 1.Through(threads).Select(_ => TaskCE.Run(UpdateEmail)).ToArray();

      changeEmailSection.EntranceGate.Open();
      changeEmailSection.EntranceGate.AwaitPassedThroughCountEqualTo(2);
      changeEmailSection.ExitGate.AwaitQueueLengthEqualTo(1);

      Thread.Sleep(100.Milliseconds());

      var bothTasksReadUserException = ExceptionCE.TryCatch(() => hasFetchedUser.Passed.Must().Be(1, "Only one thread should have been able to fetch the taggregate"));

      var bothTasksCompletedException = ExceptionCE.TryCatch(() => changeEmailSection.ExitGate.Queued.Must().Be(1, "One thread should be blocked by transaction and never reach here until the other completes the transaction."));

      changeEmailSection.Open();

      var taskException = ExceptionCE.TryCatch(() => Task.WaitAll(tasks)) as AggregateException; //Sql duplicate key (TaggregateId, Version) Exception would be thrown here if history was not serialized. Or a deadlock will be thrown if the locking is not done correctly.

      if(bothTasksCompletedException != null || taskException != null || bothTasksReadUserException != null)
         throw new AggregateException(EnumerableCE.Create(bothTasksCompletedException).Append(bothTasksReadUserException).Concat(taskException?.InnerExceptions ?? new ReadOnlyCollection<Exception>([])).Where(it => it != null).Cast<Exception>());

      UseInScope(session =>
      {
         var userHistory = ((ITeventStoreReader)session).GetHistory(user.Id)
                                                       .ToArray(); //Reading the taggregate will throw an exception if the history is invalid.
         userHistory.Length.Must()
                    .Be(threads + 2); //Make sure that all of the transactions completed
      });
      return;

      void UpdateEmail() =>
         UseInTransactionalScope(session =>
         {
            using(changeEmailSection.Enter())
            {
               var userToUpdate = session.Get<User>(user.Id);
               hasFetchedUser.AwaitPassThrough();
               userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
            }
         });
   }

   [PCT]
   public void If_an_updater_is_used_in_two_transactions_an_exception_is_thrown()
   {
      using(_serviceLocator.BeginScope())
      {
         using var updater = _serviceLocator.Resolve<ITeventStoreUpdater>();
         var user = new User();
         user.Register("email@email.se", "password", new TaggregateId());

         TransactionScopeCe.Execute(() => updater.Save(user));
         MustActions.Invoking(() => TransactionScopeCe.Execute(() => updater.Get<User>(user.Id)))
                      .Must().Throw<InvalidOperationException>();
      }
   }
}
