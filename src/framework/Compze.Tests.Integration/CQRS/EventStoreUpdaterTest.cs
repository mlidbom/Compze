﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Compze.DependencyInjection;
using Compze.Messaging;
using Compze.Messaging.Buses;
using Compze.Persistence.EventStore;
using Compze.Refactoring.Naming;
using Compze.SystemCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ThreadingCE;
using Compze.Testing;
using Compze.Testing.DependencyInjection;
using Compze.Testing.Performance;
using Compze.Testing.SystemCE;
using Compze.Testing.SystemCE.TransactionsCE;
using Compze.Testing.Threading;
using Compze.Testing.Transactions;
using FluentAssertions;
using FluentAssertions.Extensions;
using JetBrains.Annotations;
using NUnit.Framework;

// ReSharper disable AccessToDisposedClosure

namespace Compze.Tests.Integration.CQRS;

public class EventStoreUpdaterTest(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   class EventSpy
   {
      public IEnumerable<IExactlyOnceEvent> DispatchedMessages => _events.ToList();
      public void Receive(IExactlyOnceEvent @event) => _events.Add(@event);
      readonly List<IExactlyOnceEvent> _events = [];
   }

   EventSpy _eventSpy;

   IServiceLocator _serviceLocator;

   [SetUp] public void SetupBus()
   {
      _serviceLocator = TestWiringHelper.SetupTestingServiceLocator();

      _eventSpy = new EventSpy();

      _serviceLocator.Resolve<IMessageHandlerRegistrar>()
                     .ForEvent<IExactlyOnceEvent>(_eventSpy.Receive);

      _serviceLocator.Resolve<ITypeMappingRegistar>()
                     .Map<User>("2cfabb11-5e5a-494d-898f-8bfc654544eb")
                     .Map<IUserEvent>("0727c209-2f49-46ab-a56b-a1332415a895")
                     .Map<MigratedAfterUserChangedEmailEvent>("9ff42a12-f28c-447a-8aa1-79e6f685fa41")
                     .Map<MigratedBeforeUserRegisteredEvent>("3338e1d4-3839-4f63-9248-ea4dd30c8348")
                     .Map<MigratedReplaceUserChangedPasswordEvent>("45db6370-f7e7-4eb8-b792-845485d86295")
                     .Map<UserChangedEmail>("40ae1f6d-5f95-4c60-ac5f-21a3d1c85de9")
                     .Map<UserChangedPassword>("0b3b57f6-fd69-4da1-bb52-15033495f044")
                     .Map<UserEvent>("fa71e035-571d-4231-bd65-e667c138ec36")
                     .Map<UserRegistered>("03265864-8e1d-4eb7-a7a9-63dfc2b965de")
                     .Map<IMigratedAfterUserChangedEmailEvent>("4ed567a3-724a-48d5-80f2-58978ca66922")
                     .Map<IMigratedBeforeUserRegisteredEvent>("dbe74932-9a8d-4977-8f85-d55de7711f26")
                     .Map<IMigratedReplaceUserChangedPasswordEvent>("798d793d-1866-41e5-8d59-26bf285dfc80")
                     .Map<IUserChangedEmail>("27cfff73-9f21-4835-83d1-fbb4d58419e3")
                     .Map<IUserChangedPassword>("9ee2f0e9-af5c-469b-8b85-7eda6a856b81")
                     .Map<IUserRegistered>("6a9b3276-cedc-4dae-a15c-4d386c935a48");
   }

   [TearDown] public async Task TearDownTask() => await _serviceLocator.DisposeAsync();

   protected void UseInTransactionalScope([InstantHandle] Action<IEventStoreUpdater> useSession)
      => _serviceLocator.ExecuteTransactionInIsolatedScope(
         () => useSession(_serviceLocator.Resolve<IEventStoreUpdater>()));

   protected TResult UseInTransactionalScope<TResult>([InstantHandle] Func<IEventStoreUpdater, TResult> useSession)
      => _serviceLocator.ExecuteTransactionInIsolatedScope(
         () => useSession(_serviceLocator.Resolve<IEventStoreUpdater>()));

   protected void UseInScope([InstantHandle]Action<IEventStoreUpdater> useSession)
      => _serviceLocator.ExecuteInIsolatedScope(
         () => useSession(_serviceLocator.Resolve<IEventStoreUpdater>()));

   [Test]
   public void WhenFetchingAggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown() =>
      UseInTransactionalScope(session => FluentActions.Invoking(() => session.Get<User>(Guid.NewGuid()))
                                                      .Should().Throw<ArgumentOutOfRangeException>());

   [Test]
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

         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
         Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

      });
   }

   [Test]
   public void ThrowsIfUsedByMultipleThreads()
   {
      IEventStoreUpdater? updater = null;
      IEventStoreReader? reader = null;
      using var wait = new ManualResetEventSlim();
      Task.Run(() =>
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

      Assert.Throws<MultiThreadedUseException>(() => updater.Get<User>(Guid.NewGuid()));
      Assert.Throws<MultiThreadedUseException>(() => updater.Dispose());
      Assert.Throws<MultiThreadedUseException>(() => reader.GetReadonlyCopyOfVersion<User>(Guid.NewGuid(), 1));
      Assert.Throws<MultiThreadedUseException>(() => updater.Save(new User()));
      Assert.Throws<MultiThreadedUseException>(() => updater.TryGet(Guid.NewGuid(), out User? _));

   }

   [Test]
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
         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
         Assert.That(loadedUser.Password, Is.EqualTo("password"));

         loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 2);
         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
         Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));

         loadedUser = reader.GetReadonlyCopyOfVersion<User>(user.Id, 3);
         Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
         Assert.That(loadedUser.Email, Is.EqualTo("NewEmail"));
         Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
      });
   }

   [Test]
   public void ReturnsSameInstanceOnRepeatedLoads()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(session =>
      {
         var loaded1 = session.Get<User>(user.Id);
         var loaded2 = session.Get<User>(user.Id);
         Assert.That(loaded1, Is.SameAs(loaded2));
      });
   }

   [Test]
   public void ReturnsSameInstanceOnLoadAfterSave()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());

      UseInTransactionalScope(session =>
      {
         session.Save(user);

         var loaded1 = session.Get<User>(user.Id);
         var loaded2 = session.Get<User>(user.Id);
         Assert.That(loaded1, Is.SameAs(loaded2));
         Assert.That(loaded1, Is.SameAs(user));
      });
   }

   [Test]
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
         Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
      });
   }

   [Test]
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
         Assert.That(loadedUser.Email, Is.EqualTo("OriginalEmail"));
      });
   }

   [Test]
   public void ResetsAggregatesAfterSaveChanges()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));
      ((IEventStored)user).Commit(events => events.Should().BeEmpty());
   }

   [Test]
   public void ThrowsWhenAttemptingToSaveExistingAggregate()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());

      UseInTransactionalScope(session => session.Save(user));

      UseInTransactionalScope(
         session => FluentActions.Invoking(() => session.Save(user))
                                 .Should().Throw<InvalidOperationException>());
   }

   [Test]
   public void DoesNotExplodeWhenSavingMoreThan10Events()
   {
      var user = new User();
      user.Register("OriginalEmail", "password", Guid.NewGuid());
      1.Through(100).ForEach(index => user.ChangeEmail("email" + index));

      UseInTransactionalScope(session => session.Save(user));
   }

   [Test]
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
         Assert.That(loadedUser2.Id, Is.EqualTo(user2.Id));
         Assert.That(loadedUser2.Email, Is.EqualTo(user2.Email));
         Assert.That(loadedUser2.Password, Is.EqualTo(user2.Password));
      });

      UseInTransactionalScope(session => Assert.That(session.TryGet(user1.Id, out User? _), Is.False));
   }

   [Test]
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
      Assert.That(published.Last(), Is.InstanceOf<UserChangedEmail>());
   }

   [Test] public void Events_should_be_published_immediately()
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

   [Test]
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
         Assert.That(history.Count, Is.EqualTo(2));
      });
   }

   [Test]
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
         Assert.That(history.Count, Is.EqualTo(0));
      });
   }

   [Test]
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
         Assert.That(history.Count, Is.EqualTo(0));
      });
   }


   [Test, NCrunch.Framework.EnableRdi(false)]
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

   [Test]
   public void EventsArePublishedImmediatelyOnAggregateChanges()
   {
      var users = 1.Through(9).Select(i => { var u = new User(); u.Register(i + "@test.com", "abcd", Guid.NewGuid()); u.ChangeEmail("new" + i + "@test.com"); return u; }).ToList();

      UseInTransactionalScope(session =>
      {
         users.Take(3).ForEach(session.Save);
         Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(6));
      });

      UseInTransactionalScope(session =>
      {
         Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(6));
         users.Skip(3).Take(3).ForEach(session.Save);
         Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(12));
      });

      UseInTransactionalScope(session =>
      {
         Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(12));
         users.Skip(6).Take(3).ForEach(session.Save);
         Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(18));
      });

      UseInTransactionalScope(_ =>
      {
         Assert.That(_eventSpy.DispatchedMessages.Count, Is.EqualTo(18));

         var dispatchedEvents = _eventSpy.DispatchedMessages.OfType<IAggregateEvent>().ToList();
         Assert.That(dispatchedEvents.Select(e => e.MessageId).Distinct().Count(), Is.EqualTo(18));

         var allPersistedEvents = _serviceLocator.EventStore().ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();
         EventStorageTestHelper.StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(dispatchedEvents);
         EventStorageTestHelper.StripSeventhDecimalPointFromSecondFractionOnUtcUpdateTime(allPersistedEvents);

         allPersistedEvents.Should().BeEquivalentTo(dispatchedEvents, options => options.WithStrictOrdering());
      });
   }

   [Test]
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


   [Test] public void If_the_first_transaction_to_insert_an_event_of_specific_type_fails_the_next_succeeds()
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

   [Test, LongRunning]
   public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
   {
      var user = new User();
      user.Register("email@email.se", "password", Guid.NewGuid());
      UseInTransactionalScope(session =>
      {
         session.Save(user);
         user.ChangeEmail("newemail@somewhere.not");
      });

      var getHistorySection = GatedCodeSection.WithTimeout(2.Seconds());
      var changeEmailSection = GatedCodeSection.WithTimeout(2.Seconds());

      const int threads = 2;
      var tasks = 1.Through(threads).Select(_ => Task.Run(UpdateEmail)).ToArray();

      getHistorySection.LetOneThreadPass();
      changeEmailSection.LetOneThreadEnterAndReachExit();
      changeEmailSection.Open();
      getHistorySection.Open();

      Task.WaitAll(tasks);//Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized

      UseInScope(
         session =>
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

   [Test, LongRunning]
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

      var tasks = 1.Through(threads).Select(_ => Task.Run(UpdateEmail)).ToArray();

      changeEmailSection.EntranceGate.Open();
      changeEmailSection.EntranceGate.AwaitPassedThroughCountEqualTo(2);
      changeEmailSection.ExitGate.AwaitQueueLengthEqualTo(1);

      Thread.Sleep(100.Milliseconds());

      var bothTasksReadUserException = ExceptionCE.TryCatch(() => hasFetchedUser.Passed.Should().Be(1, "Only one thread should have been able to fetch the aggregate"));

      var bothTasksCompletedException = ExceptionCE.TryCatch(() => changeEmailSection.ExitGate.Queued.Should().Be(1, "One thread should be blocked by transaction and never reach here until the other completes the transaction."));

      changeEmailSection.Open();

      var taskException = ExceptionCE.TryCatch(() => Task.WaitAll(tasks)) as AggregateException;//Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized. Or a deadlock will be thrown if the locking is not done correctly.

      if(bothTasksCompletedException != null || taskException != null || bothTasksReadUserException != null)
         throw new AggregateException(EnumerableCE.Create(bothTasksCompletedException).Append(bothTasksReadUserException).Concat(taskException?.InnerExceptions ?? new ReadOnlyCollection<Exception>([])).Where(it => it != null).Cast<Exception>());

      UseInScope(
         session =>
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

   [Test]
   public void If_an_updater_is_used_in_two_transactions_an_exception_is_thrown()
   {
      using (_serviceLocator.BeginScope())
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