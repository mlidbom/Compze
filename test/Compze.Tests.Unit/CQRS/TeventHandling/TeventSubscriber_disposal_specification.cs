using Compze.Contracts.Exceptions;
using Compze.Must;

using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.xUnitBDD;

using static Compze.Must.MustActions;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

public static class TeventSubscriber_disposal_specification
{
   public class Given_a_dispatcher_with_a_handler_registered_through_each_of_two_subscribers
   {
      readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New();
      readonly ITeventSubscriber<IUserTevent> _firstSubscriber;
      int _firstSubscriberHandlerCalls;
      int _secondSubscriberHandlerCalls;

      protected Given_a_dispatcher_with_a_handler_registered_through_each_of_two_subscribers()
      {
         _firstSubscriber = _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => _firstSubscriberHandlerCalls++);
         _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => _secondSubscriberHandlerCalls++);
      }

      public class after_disposing_the_first_subscriber : Given_a_dispatcher_with_a_handler_registered_through_each_of_two_subscribers
      {
         public after_disposing_the_first_subscriber() => _firstSubscriber.Dispose();

         [XF] public void registering_another_handler_through_it_throws() =>
            Invoking(() => _firstSubscriber.For<IUserTevent.IUserRegistered>(_ => {})).Must().Throw<StateAssertionFailedException>();

         public class when_a_matching_tevent_is_dispatched : after_disposing_the_first_subscriber
         {
            public when_a_matching_tevent_is_dispatched() => _dispatcher.Dispatch(new UserRegistered());

            [XF] public void the_handler_registered_through_it_is_not_called() => _firstSubscriberHandlerCalls.Must().Be(0);
            [XF] public void the_handler_registered_through_the_other_subscriber_is_called() => _secondSubscriberHandlerCalls.Must().Be(1);
         }

         public class when_it_is_disposed_a_second_time_and_a_matching_tevent_is_dispatched : after_disposing_the_first_subscriber
         {
            public when_it_is_disposed_a_second_time_and_a_matching_tevent_is_dispatched()
            {
               _firstSubscriber.Dispose();
               _dispatcher.Dispatch(new UserRegistered());
            }

            [XF] public void the_handler_registered_through_the_other_subscriber_is_still_called() => _secondSubscriberHandlerCalls.Must().Be(1);
         }
      }
   }

   public class Given_BeforeHandlers_and_AfterHandlers_registered_through_one_subscriber_and_a_handler_through_another
   {
      readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New();
      readonly ITeventSubscriber<IUserTevent> _beforeAndAfterHandlersSubscriber;
      int _beforeHandlersCalls;
      int _afterHandlersCalls;
      int _handlerCalls;

      protected Given_BeforeHandlers_and_AfterHandlers_registered_through_one_subscriber_and_a_handler_through_another()
      {
         _beforeAndAfterHandlersSubscriber = _dispatcher.Register()
                                                        .BeforeHandlers(_ => _beforeHandlersCalls++)
                                                        .AfterHandlers(_ => _afterHandlersCalls++);
         _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => _handlerCalls++);
      }

      public class when_a_matching_tevent_is_dispatched : Given_BeforeHandlers_and_AfterHandlers_registered_through_one_subscriber_and_a_handler_through_another
      {
         public when_a_matching_tevent_is_dispatched() => _dispatcher.Dispatch(new UserRegistered());

         [XF] public void the_BeforeHandlers_run() => _beforeHandlersCalls.Must().Be(1);
         [XF] public void the_AfterHandlers_run() => _afterHandlersCalls.Must().Be(1);
         [XF] public void the_handler_runs() => _handlerCalls.Must().Be(1);
      }

      public class when_a_matching_tevent_is_dispatched_after_disposing_that_subscriber : Given_BeforeHandlers_and_AfterHandlers_registered_through_one_subscriber_and_a_handler_through_another
      {
         public when_a_matching_tevent_is_dispatched_after_disposing_that_subscriber()
         {
            _beforeAndAfterHandlersSubscriber.Dispose();
            _dispatcher.Dispatch(new UserRegistered());
         }

         [XF] public void its_BeforeHandlers_no_longer_run() => _beforeHandlersCalls.Must().Be(0);
         [XF] public void its_AfterHandlers_no_longer_run() => _afterHandlersCalls.Must().Be(0);
         [XF] public void the_other_subscribers_handler_still_runs() => _handlerCalls.Must().Be(1);
      }
   }

   public class Given_a_dispatcher_that_has_already_dispatched_to_a_handler_registered_through_a_subscriber
   {
      readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New();
      readonly ITeventSubscriber<IUserTevent> _originalSubscriber;
      int _originalHandlerCalls;

      protected Given_a_dispatcher_that_has_already_dispatched_to_a_handler_registered_through_a_subscriber()
      {
         _originalSubscriber = _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => _originalHandlerCalls++);
         _dispatcher.Dispatch(new UserRegistered());
      }

      public class when_that_subscriber_is_disposed_and_a_new_subscriber_registers_a_handler_and_the_same_tevent_type_is_dispatched_again : Given_a_dispatcher_that_has_already_dispatched_to_a_handler_registered_through_a_subscriber
      {
         int _newHandlerCalls;

         public when_that_subscriber_is_disposed_and_a_new_subscriber_registers_a_handler_and_the_same_tevent_type_is_dispatched_again()
         {
            _originalSubscriber.Dispose();
            _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => _newHandlerCalls++);
            _dispatcher.Dispatch(new UserRegistered());
         }

         [XF] public void the_disposed_subscribers_handler_is_not_called_again() => _originalHandlerCalls.Must().Be(1);
         [XF] public void the_new_subscribers_handler_is_called() => _newHandlerCalls.Must().Be(1);
      }
   }

   public class Given_a_dispatcher_whose_only_handler_was_registered_through_a_single_subscriber
   {
      readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New();
      readonly ITeventSubscriber<IUserTevent> _subscriber;

      protected Given_a_dispatcher_whose_only_handler_was_registered_through_a_single_subscriber()
         => _subscriber = _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => {});

      public class when_the_subscriber_is_disposed : Given_a_dispatcher_whose_only_handler_was_registered_through_a_single_subscriber
      {
         public when_the_subscriber_is_disposed() => _subscriber.Dispose();

         [XF] public void dispatching_throws_TeventUnhandledException() =>
            Invoking(() => _dispatcher.Dispatch(new UserRegistered())).Must().Throw<TeventUnhandledException>();
      }
   }

   public class Given_a_handler_that_disposes_its_own_subscriber_when_called_and_a_second_subscribers_handler_registered_after_it
   {
      readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New();
      readonly ITeventSubscriber<IUserTevent> _selfDisposingSubscriber;
      int _selfDisposingHandlerCalls;
      int _otherHandlerCalls;

      protected Given_a_handler_that_disposes_its_own_subscriber_when_called_and_a_second_subscribers_handler_registered_after_it()
      {
         _selfDisposingSubscriber = _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ =>
         {
            _selfDisposingHandlerCalls++;
            _selfDisposingSubscriber?.Dispose();
         });
         _dispatcher.Register().For<IUserTevent.IUserRegistered>(_ => _otherHandlerCalls++);
      }

      public class when_a_matching_tevent_is_dispatched_twice : Given_a_handler_that_disposes_its_own_subscriber_when_called_and_a_second_subscribers_handler_registered_after_it
      {
         public when_a_matching_tevent_is_dispatched_twice()
         {
            _dispatcher.Dispatch(new UserRegistered());
            _dispatcher.Dispatch(new UserRegistered());
         }

         [XF] public void the_self_disposing_handler_is_called_only_by_the_first_dispatch() => _selfDisposingHandlerCalls.Must().Be(1);
         [XF] public void the_other_subscribers_handler_is_called_by_both_dispatches() => _otherHandlerCalls.Must().Be(2);
      }
   }

   interface IUserTevent : ITaggregateTevent
   {
      interface IUserRegistered : IUserTevent;
   }

   class UserRegistered : TaggregateTevent, IUserTevent.IUserRegistered;
}
