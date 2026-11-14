using Compze.Must;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.Threading;
using Compze.Threading.Testing;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

public static class MutableTeventDispatcher_used_from_several_threads_specification
{
   public class Given_200_fresh_dispatchers_each_with_a_handler_for_each_of_eight_tevent_types
   {
      const int NumberOfDispatchers = 200;
      const int NumberOfDispatchingThreads = 8;

      static readonly IAccountTevent[] OneTeventOfEachType = [new Created(), new EmailChanged(), new PasswordChanged(), new LoggedIn(), new LoggedOut(), new Locked(), new Unlocked(), new Deleted()];

      readonly IReadOnlyList<IMutableTeventDispatcher<IAccountTevent>> _dispatchers;
      int _handlerCalls;

      protected Given_200_fresh_dispatchers_each_with_a_handler_for_each_of_eight_tevent_types() =>
         _dispatchers = [..1.Through(NumberOfDispatchers).Select(_ => DispatcherWithAHandlerForEachTeventType())];

      IMutableTeventDispatcher<IAccountTevent> DispatcherWithAHandlerForEachTeventType()
      {
         var dispatcher = IMutableTeventDispatcher<IAccountTevent>.New();
         dispatcher.Register()
                   .For<IAccountTevent.ICreated>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.IEmailChanged>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.IPasswordChanged>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.ILoggedIn>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.ILoggedOut>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.ILocked>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.IUnlocked>(_ => CountTheHandlerCall())
                   .For<IAccountTevent.IDeleted>(_ => CountTheHandlerCall());
         return dispatcher;
      }

      void CountTheHandlerCall() => Interlocked.Increment(ref _handlerCalls);

      ///<summary>Releases <see cref="NumberOfDispatchingThreads"/> threads at once, each dispatching a tevent of every type to every dispatcher, each thread starting<br/>
      /// with a different type so that the threads resolve the handlers of different types on the same dispatcher at the same time. Releases<br/>
      /// <paramref name="numberOfSubscribingThreads"/> more threads with them, each registering and disposing a subscriber on every dispatcher.</summary>
      protected void DispatchFromEightThreadsReleasedAtOnceWhile(int numberOfSubscribingThreads)
      {
         var startingLine = IThreadGate.NewClosed(WaitTimeout.Seconds(10));
         using var taskRunner = TestingTaskRunner.WithTimeout(TimeSpan.FromSeconds(30));
         taskRunner.Run([..0.Through(NumberOfDispatchingThreads - 1).Select(DispatchATeventOfEveryTypeToEveryDispatcherStartingWithTheTypeAt)]);
         taskRunner.Run([..Enumerable.Repeat<Action>(RegisterAndDisposeASubscriberOnEveryDispatcher, numberOfSubscribingThreads)]);
         startingLine.AwaitQueueLengthEqualTo(NumberOfDispatchingThreads + numberOfSubscribingThreads);
         startingLine.Open();

         return;

         Action DispatchATeventOfEveryTypeToEveryDispatcherStartingWithTheTypeAt(int firstTypeIndex) => () =>
         {
            IAccountTevent[] teventsInTheOrderThisThreadDispatchesThem = [..OneTeventOfEachType.Skip(firstTypeIndex), ..OneTeventOfEachType.Take(firstTypeIndex)];
            startingLine.AwaitPassThrough();
            foreach(var dispatcher in _dispatchers)
            {
               foreach(var tevent in teventsInTheOrderThisThreadDispatchesThem) dispatcher.Dispatch(tevent);
            }
         };

         void RegisterAndDisposeASubscriberOnEveryDispatcher()
         {
            startingLine.AwaitPassThrough();
            foreach(var dispatcher in _dispatchers) dispatcher.Register().For<IAccountTevent.IEmailChanged>(_ => {}).Dispose();
         }
      }

      static int OneHandlerCallPerDispatchedTevent => NumberOfDispatchers * NumberOfDispatchingThreads * OneTeventOfEachType.Length;

      public class when_eight_threads_released_at_once_each_dispatch_a_tevent_of_every_type_to_every_dispatcher
         : Given_200_fresh_dispatchers_each_with_a_handler_for_each_of_eight_tevent_types
      {
         public when_eight_threads_released_at_once_each_dispatch_a_tevent_of_every_type_to_every_dispatcher() =>
            DispatchFromEightThreadsReleasedAtOnceWhile(numberOfSubscribingThreads: 0);

         [XF] public void every_dispatched_tevent_reaches_its_handler() => _handlerCalls.Must().Be(OneHandlerCallPerDispatchedTevent);
      }

      public class when_eight_threads_released_at_once_each_dispatch_a_tevent_of_every_type_to_every_dispatcher_while_two_more_register_and_dispose_subscribers
         : Given_200_fresh_dispatchers_each_with_a_handler_for_each_of_eight_tevent_types
      {
         public when_eight_threads_released_at_once_each_dispatch_a_tevent_of_every_type_to_every_dispatcher_while_two_more_register_and_dispose_subscribers() =>
            DispatchFromEightThreadsReleasedAtOnceWhile(numberOfSubscribingThreads: 2);

         [XF] public void every_dispatched_tevent_reaches_its_handler() => _handlerCalls.Must().Be(OneHandlerCallPerDispatchedTevent);
      }
   }

   public class Given_a_dispatcher_whose_handler_for_one_tevent_type_waits_at_a_gate
   {
      readonly IThreadGate _insideTheHandler = IThreadGate.NewClosed(WaitTimeout.Seconds(10));
      readonly IMutableTeventDispatcher<IAccountTevent> _dispatcher = IMutableTeventDispatcher<IAccountTevent>.New();

      protected Given_a_dispatcher_whose_handler_for_one_tevent_type_waits_at_a_gate() =>
         _dispatcher.Register()
                    .For<IAccountTevent.ICreated>(_ => _insideTheHandler.AwaitPassThrough())
                    .For<IAccountTevent.IEmailChanged>(_ => {});

      public class when_one_thread_is_inside_that_handler_and_another_dispatches_and_registers_and_disposes_a_subscriber : Given_a_dispatcher_whose_handler_for_one_tevent_type_waits_at_a_gate
      {
         readonly bool _theOtherThreadFinishedWhileTheFirstWasInsideTheHandler;

         public when_one_thread_is_inside_that_handler_and_another_dispatches_and_registers_and_disposes_a_subscriber()
         {
            using var taskRunner = TestingTaskRunner.WithTimeout(TimeSpan.FromSeconds(10));
            taskRunner.Run(() => _dispatcher.Dispatch(new Created()));
            _insideTheHandler.AwaitQueueLengthEqualTo(1);

            var theOtherThread = taskRunner.Run(() =>
            {
               _dispatcher.Dispatch(new EmailChanged());
               _dispatcher.Register().For<IAccountTevent.IEmailChanged>(_ => {}).Dispose();
            });
            _theOtherThreadFinishedWhileTheFirstWasInsideTheHandler = theOtherThread.Wait(TimeSpan.FromSeconds(5));

            _insideTheHandler.Open();
         }

         [XF] public void the_other_thread_is_not_held_up_by_the_handler() => _theOtherThreadFinishedWhileTheFirstWasInsideTheHandler.Must().BeTrue();
      }
   }

   interface IAccountTevent : ITaggregateTevent
   {
      interface ICreated : IAccountTevent;
      interface IEmailChanged : IAccountTevent;
      interface IPasswordChanged : IAccountTevent;
      interface ILoggedIn : IAccountTevent;
      interface ILoggedOut : IAccountTevent;
      interface ILocked : IAccountTevent;
      interface IUnlocked : IAccountTevent;
      interface IDeleted : IAccountTevent;
   }

   class Created : TaggregateTevent, IAccountTevent.ICreated;
   class EmailChanged : TaggregateTevent, IAccountTevent.IEmailChanged;
   class PasswordChanged : TaggregateTevent, IAccountTevent.IPasswordChanged;
   class LoggedIn : TaggregateTevent, IAccountTevent.ILoggedIn;
   class LoggedOut : TaggregateTevent, IAccountTevent.ILoggedOut;
   class Locked : TaggregateTevent, IAccountTevent.ILocked;
   class Unlocked : TaggregateTevent, IAccountTevent.IUnlocked;
   class Deleted : TaggregateTevent, IAccountTevent.IDeleted;
}
