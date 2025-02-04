﻿using Compze.Messaging.Events;
using Compze.Persistence.EventStore;
using Compze.Testing.TestFrameworkExtensions.XUnit;
using FluentAssertions;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0051 //Review OK: unused private members are intentional in this test.
#pragma warning disable IDE1006 //Review OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.EventHandling;

public static class MutableEventDispatcher_specification
{
   public class Given_an_instance
   {
      readonly IMutableEventDispatcher<IUserEvent> _dispatcher = IMutableEventDispatcher<IUserEvent>.New();

      public class with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_event_type : Given_an_instance
      {
         int CallsMade { get; set; }

         int? BeforeHandlers1CallOrder { get; set; }
         int? BeforeHandlers2CallOrder { get; set; }

         int? UserCreatedCallOrder { get; set; }

         int? AfterHandlers1CallOrder { get; set; }
         int? AfterHandlers2CallOrder { get; set; }

         public with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_event_type()
         {
            _dispatcher.Register()
                       .IgnoreUnhandled<IIgnoredUserEvent>()
                       .BeforeHandlers(_ => BeforeHandlers1CallOrder = ++CallsMade)
                       .BeforeHandlers(_ => BeforeHandlers2CallOrder = ++CallsMade)
                       .AfterHandlers(_ => AfterHandlers1CallOrder = ++CallsMade)
                       .AfterHandlers(_ => AfterHandlers2CallOrder = ++CallsMade)
                       .For<IUserCreatedEvent>(_ => UserCreatedCallOrder = ++CallsMade)
                       .For<IUserRegistered>(_ => ++CallsMade)
                       .For<IUserSkillsRemoved>(_ => ++CallsMade)
                       .For<IUserSkillsAdded>(_ => ++CallsMade);
         }

         [XFact] public void when_dispatching_an_ignored_event_no_calls_are_made_to_any_handlers()
         {
            _dispatcher.Dispatch(new IgnoredUserEvent());
            CallsMade.Should().Be(0);
         }

         [XFact] public void when_dispatching_an_unhandled_event_that_is_not_ignored_an_exception_is_thrown() =>
            Assert.ThrowsAny<EventUnhandledException>(() => _dispatcher.Dispatch(new UnHandledUserEvent()));

         public class when_dispatching_an_IUserCreatedEvent : with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_event_type
         {
            public when_dispatching_an_IUserCreatedEvent() => _dispatcher.Dispatch(new UserCreatedEvent());

            [XFact] public void BeforeHandler1_is_called_first() => BeforeHandlers1CallOrder.Should().Be(1);
            [XFact] public void BeforeHandler2_is_called_second() => BeforeHandlers2CallOrder.Should().Be(2);
            [XFact] public void The_specific_handler_is_called_third() => UserCreatedCallOrder.Should().Be(3);
            [XFact] public void AfterHandler1_is_called_fourth() => AfterHandlers1CallOrder.Should().Be(4);
            [XFact] public void AfterHandler2_is_called_fifth() => AfterHandlers2CallOrder.Should().Be(5);
            [XFact] public void Five_calls_are_made_in_total() => CallsMade.Should().Be(5);
         }
      }

      public class with_2_registered_handlers_for_the_same_event_type_then_when_dispatching_event : Given_an_instance
      {
         [XFact] public void handlers_are_called_in_registration_order()
         {
            var calls = 0;
            var handler1CallOrder = 0;
            var handler2CallOrder = 0;

            _dispatcher.Register()
                       .For<IUserRegistered>(_ => handler1CallOrder = ++calls)
                       .For<IUserRegistered>(_ => handler2CallOrder = ++calls);

            _dispatcher.Dispatch(new UserRegistered());

            handler1CallOrder.Should().Be(1);
            handler2CallOrder.Should().Be(2);
         }
      }

      interface IUserEvent : IAggregateEvent;
      interface IUserCreatedEvent : IUserEvent;
      interface IUserRegistered : IUserCreatedEvent;
      interface IUserSkillsEvent : IUserEvent;
      interface IUserSkillsAdded : IUserSkillsEvent;
      interface IUserSkillsRemoved : IUserSkillsEvent;
      interface IIgnoredUserEvent : IUserEvent;

      class UnHandledUserEvent : AggregateEvent, IUserEvent;

      class IgnoredUserEvent : AggregateEvent, IIgnoredUserEvent;

      class UserCreatedEvent : AggregateEvent, IUserCreatedEvent;

      class UserRegistered : AggregateEvent, IUserRegistered;
   }
}