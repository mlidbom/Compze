using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using JetBrains.Annotations;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.TransactionsCE;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Performance;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.SystemCE;
using Compze.Utilities.Threading.Testing;
using Compze.Utilities.SystemCE.TransactionsCE.Testing;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Tests.Infrastructure.XUnit.PluggableComponents;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.TasksCE;
using EnumerableCE = Compze.Utilities.SystemCE.LinqCE.EnumerableCE;
using Compze.Wiring.Testing.Sql;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Integration.CQRS;

public class EventStoreUpdaterTest : UniversalTestBase
{
   class EventSpy
   {
      public IEnumerable<IExactlyOnceEvent> DispatchedMessages => _events.ToList();
      public void Receive(IExactlyOnceEvent @event) => _events.Add(@event);
      readonly List<IExactlyOnceEvent> _events = [];
   }

   readonly EventSpy _eventSpy;
   readonly IServiceLocator _serviceLocator;

   public EventStoreUpdaterTest()
   {
      _serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator(null);

      _eventSpy = new EventSpy();

      _serviceLocator.Resolve<IMessageHandlerRegistrar>()
                     .ForEvent<IExactlyOnceEvent>(_eventSpy.Receive);
   }

   protected override async Task DisposeAsyncInternal() => await _serviceLocator.DisposeAsync().AsTask();

   protected void UseInTransactionalScope([InstantHandle] Action<IEventStoreUpdater> useSession)
      => _serviceLocator.ExecuteTransactionInIsolatedScope(() => useSession(_serviceLocator.Resolve<IEventStoreUpdater>()));

   protected TResult UseInTransactionalScope<TResult>([InstantHandle] Func<IEventStoreUpdater, TResult> useSession)
      => _serviceLocator.ExecuteTransactionInIsolatedScope(() => useSession(_serviceLocator.Resolve<IEventStoreUpdater>()));

   public void UseInScope([InstantHandle] Action<IEventStoreUpdater> useSession)
      => _serviceLocator.ExecuteInIsolatedScope(() => useSession(_serviceLocator.Resolve<IEventStoreUpdater>()));

   [PCT]
   public void WhenFetchingAggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown()
   {
      UseInTransactionalScope(session => FluentActions.Invoking(() => session.Get<User>(Guid.NewGuid()))
                                                      .Should().Throw<ArgumentOutOfRangeException>());
   }

   [PCT]
   public void CanSaveAndLoadAggregate()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());
      user.ChangePassword("NewPassword");
      user.ChangeEmail("NewEmail");

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);

         loadedUser.Id.Should().Be(user.Id);
         loadedUser.Email.Should().Be(user.Email);
         loadedUser.Password.Should().Be(user.Password);
      });
   }

   [PCT]
   public void ThrowsIfUsedByMultipleThreads()
   {
      IEventStoreUpdater? updater = null;
      IEventStoreReader? reader = null;
      using var wait = new ManualResetEventSlim();
      TaskCE.Run(() =>
      {
         _serviceLocator.ExecuteInIsolatedScope(() =>
         {
            updater = _serviceLocator.Resolve<IEventStoreUpdater>();
            reader = _serviceLocator.Resolve<IEventStoreReader>();
         });
         wait.Set();
      });
      wait.Wait();
      updater = updater.NotNull();
      reader = reader.NotNull();

      FluentActions.Invoking(() => updater.Get<User>(Guid.NewGuid())).Should().Throw<MultiThreadedUseException>();
      FluentActions.Invoking(() => updater.Dispose()).Should().Throw<MultiThreadedUseException>();
      FluentActions.Invoking(() => reader.GetReadonlyCopyOfVersion<User>(Guid.NewGuid(), 1)).Should().Throw<MultiThreadedUseException>();
      FluentActions.Invoking(() => updater.Save(new User())).Should().Throw<MultiThreadedUseException>();
      FluentActions.Invoking(() => updater.TryGet(Guid.NewGuid(), out User? _)).Should().Throw<MultiThreadedUseException>();
   }

   [PCT]
   public void CanLoadSpecificVersionOfAggregate()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());
      user.ChangePassword("NewPassword");
      user.ChangeEmail("NewEmail");

      UseInTransactionalScope(session => session.Save(user));

      UseInScope(_ =>
      {
         var reader = _serviceLocator.Resolve<IEventStoreReader>();
         var loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 1);
         loadedUser.Id.Should().Be(user.Id);
         loadedUser.Email.Should().Be("email@email.se");
         loadedUser.Password.Should().Be("password");

         loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 2);
         loadedUser.Id.Should().Be(user.Id);
         loadedUser.Email.Should().Be("email@email.se");
         loadedUser.Password.Should().Be("NewPassword");

         loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 3);
         loadedUser.Id.Should().Be(user.Id);
         loadedUser.Email.Should().Be("NewEmail");
         loadedUser.Password.Should().Be("NewPassword");
      });
   }

   [PCT]
   public void ReturnsSameInstanceOnRepeatedLoads()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loaded1 = session.Get<User>(user.Id);
         var loaded2 = session.Get<User>(user.Id);
         loaded1.Should().BeSameAs(loaded2);
      });
   }

   [PCT]
   public void ReturnsSameInstanceOnLoadAfterSave()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session =>
      {
         session.Save(user);

         var loaded1 = session.Get<User>(user.Id);
         var loaded2 = session.Get<User>(user.Id);
         loaded1.Should().BeSameAs(loaded2);
         loaded1.Should().BeSameAs(user);
      });
   }

   [PCT]
   public void TracksAndUpdatesLoadedAggregates()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);
         loadedUser.ChangePassword("NewPassword");
      });

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);
         loadedUser.Password.Should().Be("NewPassword");
      });
   }

   [PCT]
   public void DoesNotUpdateAggregatesLoadedViaSpecificVersion()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(_ =>
      {
         var loadedUser = _serviceLocator.Resolve<IEventStoreReader>().GetReadonlyCopyOfVersion<User>(user.Id, 1);
         loadedUser.ChangeEmail("NewEmail");
      });

      UseInTransactionalScope(session =>
      {
         var loadedUser = session.Get<User>(user.Id);
         loadedUser.Email.Should().Be("OriginalEmail");
      });
   }

   [PCT]
   public void ResetsAggregatesAfterSaveChanges()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));
      ((IEventStored)user).Commit(events => events.Should().BeEmpty());
   }

   [PCT]
   public void ThrowsWhenAttemptingToSaveExistingAggregate()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session => FluentActions.Invoking(() => session.Save(user))
                                                      .Should().Throw<InvalidOperationException>());
   }

   [PCT]
   public void DoesNotExplodeWhenSavingMoreThan10Events()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());
      1.Through(100).ForEach(index => user.ChangeEmail("email" + index));

      UseInTransactionalScope(session => session.Save(user));
   }

   [PCT]
   public void AggregateCannotBeRetrievedAfterBeingDeleted()
   {
      var user1 = new User();
      user1.Register("email1@email.se", "password", Guid.NewGuid());

      var user2 = new User();
      user2.Register("email2@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session =>
      {
         session.Save(user1);
         session.Save(user2);
      });

      UseInTransactionalScope(session =>
      {
         session.Delete(user1.Id);

         var loadedUser2 = session.Get<User>(user2.Id);
         loadedUser2.Id.Should().Be(user2.Id);
         loadedUser2.Email.Should().Be(user2.Email);
         loadedUser2.Password.Should().Be(user2.Password);
      });

      UseInTransactionalScope(session => session.TryGet(user1.Id, out User? _).Should().BeFalse());
   }

   [PCT]
   public void DeletingAnAggregateDoesNotPreventEventsFromItFromBeingRaised()
   {
      var user1 = new User();
      user1.Register("email1@email.se", "password", Guid.NewGuid());

      var user2 = new User();
      user2.Register("email2@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session =>
      {
         session.Save(user1);
         session.Save(user2);
      });

      _eventSpy.DispatchedMessages.Count().Should().Be(2);

      UseInTransactionalScope(session =>
      {
         user1 = session.Get<User>(user1.Id);

         user1.ChangeEmail("new_email");

         session.Delete(user1.Id);
      });

      var published = _eventSpy.DispatchedMessages.ToList();
      _eventSpy.DispatchedMessages.Count()
               .Should()
               .Be(3);
      published.Last().Should().BeOfType<UserChangedEmail>();
   }

   [PCT]
   public void Events_should_be_published_immediately()
   {
      UseInTransactionalScope(session =>
      {
         var user1 = new User();
         user1.Register("email1@email.se", "password", Guid.NewGuid());
         session.Save(user1);

         _eventSpy.DispatchedMessages.Last()
                  .Should()
                  .BeOfType<UserRegistered>();

         user1 = session.Get<User>(user1.Id);
         user1.ChangeEmail("new_email");
         _eventSpy.DispatchedMessages.Last()
                  .Should()
                  .BeOfType<UserChangedEmail>();
      });
   }

   [PCT]
   public void When_fetching_history_from_the_same_instance_after_updating_an_aggregate_the_fetched_history_includes_the_new_events()
   {
      var userId = Guid.NewGuid();
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
         var history = ((IEventStoreReader)session).GetHistory(userId);
         history.Count.Should().Be(2);
      });
   }

   [PCT]
   public void When_deleting_and_then_fetching_an_aggregates_history_the_history_should_be_gone()
   {
      var userId = Guid.NewGuid();

      UseInTransactionalScope(session =>
      {
         var user = new User();
         user.Register("test@email.com", "Password1", userId);
         session.Save(user);
      });

      UseInTransactionalScope(session => session.Delete(userId));

      UseInScope(session =>
      {
         var history = ((IEventStoreReader)session).GetHistory(userId);
         history.Count.Should().Be(0);
      });
   }

   [PCT]
   public void When_fetching_and_deleting_an_aggregate_then_fetching_history_again_the_history_should_be_gone()
   {
      var userId = Guid.NewGuid();

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
         var history = ((IEventStoreReader)session).GetHistory(userId);
         history.Count.Should().Be(0);
      });
   }

   //Sqlite is not really designed for high concurrency, we have not been able to get this working with SQLite
   [PCT(Exclude = [nameof(SqlLayer.Sqlite), nameof(SqlLayer.SqliteMemory)])]
   public void Concurrent_read_only_access_to_aggregate_history_can_occur_in_parallel()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

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

      timingsSummary.IndividualExecutionTimes.Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2).Should().BeGreaterThan(timingsSummary.Total, "If the sum elapsed time of the parts that run in parallel is not greater than the clock time passed parallelism is not taking place.");
      return;

      void ReadUserHistory() =>
         UseInTransactionalScope(session =>
         {
            ((IEventStoreReader)session).GetHistory(user.Id);
            Thread.Sleep(delayEachTransactionBy);
         });
   }

   [PCT]
   public void EventsArePublishedImmediatelyOnAggregateChanges()
   {
      var users = 1.Through(9).Select(i =>
      {
         var u = new User();
         u.Register(i + "@test.com", "abcd", Guid.NewGuid());
         u.ChangeEmail("new" + i + "@test.com");
         return u;
      }).ToList();

      UseInTransactionalScope(session =>
      {
         users.Take(3).ForEach(session.Save);
         _eventSpy.DispatchedMessages.Count().Should().Be(6);
      });

      UseInTransactionalScope(session =>
      {
         _eventSpy.DispatchedMessages.Count().Should().Be(6);
         users.Skip(3).Take(3).ForEach(session.Save);
         _eventSpy.DispatchedMessages.Count().Should().Be(12);
      });

      UseInTransactionalScope(session =>
      {
         _eventSpy.DispatchedMessages.Count().Should().Be(12);
         users.Skip(6).Take(3).ForEach(session.Save);
         _eventSpy.DispatchedMessages.Count().Should().Be(18);
      });

      UseInTransactionalScope(_ =>
      {
         _eventSpy.DispatchedMessages.Count().Should().Be(18);

         var dispatchedEvents = _eventSpy.DispatchedMessages.OfType<IAggregateEvent>().ToList();
         dispatchedEvents.Select(e => e.MessageId).Distinct().Count().Should().Be(18);

         var allPersistedEvents = _serviceLocator.EventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();
         EventStorageTestHelper.StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(dispatchedEvents);
         EventStorageTestHelper.StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(allPersistedEvents);

         allPersistedEvents.Should().BeEquivalentTo(dispatchedEvents, options => options.WithStrictOrdering());
      });
   }

   [PCT]
   public async Task InsertNewEventType_should_not_throw_exception_if_the_event_type_has_been_inserted_by_something_else()
   {
      var user = UseInTransactionalScope(session => User.Register(session, "email@email.se", "password", Guid.NewGuid()));
      var otherUser = await ChangeAnotherUsersEmailInOtherInstance();

      UseInTransactionalScope(session => session.Get<User>(otherUser.Id).Email.Should().Be("otheruser@email.new"));

      UseInTransactionalScope(_ => user.ChangeEmail("some@email.new"));
      return;

      async Task<User> ChangeAnotherUsersEmailInOtherInstance()
      {
         var clonedServiceLocator = _serviceLocator.Clone();
         await using var serviceLocator = clonedServiceLocator;
         return clonedServiceLocator.ExecuteTransactionInIsolatedScope(() =>
         {
            // ReSharper disable once AccessToDisposedClosure
            var session = clonedServiceLocator.Resolve<IEventStoreUpdater>();
            var another = User.Register(session,
                                        "email@email.se",
                                        "password",
                                        Guid.NewGuid());
            another.ChangeEmail("otheruser@email.new");
            return another;
         });
      }
   }

   [PCT]
   public void If_the_first_transaction_to_insert_an_event_of_specific_type_fails_the_next_succeeds()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      FluentActions.Invoking(() => ChangeUserEmail(failOnPrepare: true))
                   .Should().Throw<Exception>();

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
   public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());
      UseInTransactionalScope(session =>
      {
         session.Save(user);
         user.ChangeEmail("newemail@somewhere.not");
      });

      var getHistorySection = GatedCodeSection.WithTimeout(TimeSpanCE.Seconds(2));
      var changeEmailSection = GatedCodeSection.WithTimeout(TimeSpanCE.Seconds(2));

      const int threads = 2;
      var tasks = 1.Through(threads).Select(_ => TaskCE.Run(UpdateEmail)).ToArray();

      getHistorySection.LetOneThreadPass();
      changeEmailSection.LetOneThreadEnterAndReachExit();
      changeEmailSection.Open();
      getHistorySection.Open();

      Task.WaitAll(tasks); //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized

      UseInScope(session =>
      {
         var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                       .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
         userHistory.Length.Should()
                    .Be(threads + 2); //Make sure that all of the transactions completed
      });
      return;

      void UpdateEmail() =>
         UseInScope(session =>
         {
            using(getHistorySection.Enter())
            {
               ((IEventStoreReader)session).GetHistory(user.Id);
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

    //We have not been able to get this to work with SQLite, and since it is testing concurrency behavior is it somewhat outside of SQLite aims anyway...
    [PCT(Exclude = [nameof(SqlLayer.Sqlite), nameof(SqlLayer.SqliteMemory)])]
   public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed()
   {

      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());
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

      var bothTasksReadUserException = ExceptionCE.TryCatch(() => hasFetchedUser.Passed.Should().Be(1, "Only one thread should have been able to fetch the aggregate"));

      var bothTasksCompletedException = ExceptionCE.TryCatch(() => changeEmailSection.ExitGate.Queued.Should().Be(1, "One thread should be blocked by transaction and never reach here until the other completes the transaction."));

      changeEmailSection.Open();

      var taskException = ExceptionCE.TryCatch(() => Task.WaitAll(tasks)) as AggregateException; //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized. Or a deadlock will be thrown if the locking is not done correctly.

      if(bothTasksCompletedException != null || taskException != null || bothTasksReadUserException != null)
         throw new AggregateException(EnumerableCE.Create(bothTasksCompletedException).Append(bothTasksReadUserException).Concat(taskException?.InnerExceptions ?? new ReadOnlyCollection<Exception>([])).Where(it => it != null).Cast<Exception>());

      UseInScope(session =>
      {
         var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                       .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
         userHistory.Length.Should()
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
         using var updater = _serviceLocator.Resolve<IEventStoreUpdater>();
         var user = new User();
         user.Register("email@email.se", "password", Guid.NewGuid());

         TransactionScopeCe.Execute(() => updater.Save(user));
         FluentActions.Invoking(() => TransactionScopeCe.Execute(() => updater.Get<User>(user.Id)))
                      .Should().Throw<InvalidOperationException>();
      }
   }
}
