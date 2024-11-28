﻿using Composable.Messaging;
using Composable.Messaging.Events;
using Composable.Persistence.EventStore;
using Composable.Testing;
using NUnit.Framework;

namespace Composable.Tests.CQRS.EventHandling;

[TestFixture]public class CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests : UniversalTestBase
{
   interface IUserWrapperEvent<out TEvent> : IWrapperEvent<TEvent> where TEvent : IUserEvent {}
   class UserWrapperEvent<TEvent> : MessageTypes.WrapperEvent<TEvent>, IUserWrapperEvent<TEvent> where TEvent : IUserEvent
   {
      public UserWrapperEvent(TEvent @event) : base(@event) {}
   }

   interface IUserEvent : IAggregateEvent {}
   interface IUserCreatedEvent : IUserEvent {}
   class UserCreatedEvent : AggregateEvent, IUserCreatedEvent {}


   interface IAdminUserWrapperEvent<out TEvent> : IUserWrapperEvent<TEvent> where TEvent : IUserEvent {}
   class AdminUserWrapperEvent<TEvent> : UserWrapperEvent<TEvent>, IAdminUserWrapperEvent<TEvent> where TEvent : IUserEvent
   {
      public AdminUserWrapperEvent(TEvent @event) : base(@event) {}
   }

   interface IAdminUserEvent : IUserEvent {}
   interface IAdminUserCreatedEvent : IAdminUserEvent, IUserCreatedEvent {}
   class AdminUserCreatedEvent : AggregateEvent, IAdminUserCreatedEvent {}


   CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent> _dispatcher;
   [SetUp] public void SetupTask() => _dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent>();

   public class Publishing_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new UserCreatedEvent());

      public class Dispatches_to_handler_for : Publishing_UserCreatedEvent
      {
         [Test] public void IEvent() => AssertUserCreatedEvent().DispatchesTo<IEvent>();
         [Test] public void IUserEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserEvent>();
         [Test] public void IUserCreatedEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [Test] public void IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [Test] public void IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [Test] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedEvent
      {
         [Test] public void IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperEvent<IUserEvent>>();
      }
   }

   public class Publishing_WrapperEvent_of_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new MessageTypes.WrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
      {
         [Test] public void IEvent() => AssertUserCreatedEvent().DispatchesTo<IEvent>();
         [Test] public void IUserEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserEvent>();
         [Test] public void IUserCreatedEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [Test] public void IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [Test] public void IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [Test] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
      {
         [Test] public void IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperEvent<IUserEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
      }
   }

   public class Publishing_UserWrapperEvent_of_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new UserWrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
      {
         [Test] public void IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IEvent>();
         [Test] public void IUserEvent_() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserEvent>();
         [Test] public void IUserCreatedEvent_() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [Test] public void IWrapperEvent_of_IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [Test] public void IWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [Test] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
         [Test] public void IUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
         [Test] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
      {
         [Test] public void _IAdminUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
      }
   }

   public class Publishing_AdminUserWrapperEvent_of_UserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertAdminUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new AdminUserWrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperEvent_of_UserCreatedEvent
      {
         [Test] public void IEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IEvent>();
         [Test] public void IUserEvent_() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserEvent>();
         [Test] public void IUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [Test] public void IWrapperEvent_of_IEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [Test] public void IWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [Test] public void IWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
         [Test] public void IUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
         [Test] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
      }
   }

   public class Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent : CallMatchingHandlersInRegistrationOrderEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent() => _dispatcher.Assert().Event(new AdminUserWrapperEvent<AdminUserCreatedEvent>(new AdminUserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent
      {
         [Test] public void IEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IEvent>();
         [Test] public void IUserEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IUserEvent>();
         [Test] public void IUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [Test] public void IAdminUserEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IAdminUserEvent>();
         [Test] public void IAdminUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IAdminUserCreatedEvent>();
         [Test] public void IWrapperEvent_of_IEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [Test] public void IWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [Test] public void IWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
         [Test] public void IUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
         [Test] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IAdminUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperEvent<IAdminUserEvent>>();
         [Test] public void IAdminUserWrapperEvent_of_IAdminUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IAdminUserCreatedEvent>>();
      }
   }
}