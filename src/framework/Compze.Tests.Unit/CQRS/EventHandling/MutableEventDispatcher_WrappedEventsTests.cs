﻿using Compze.Messaging;
using Compze.Messaging.Events;
using Compze.Persistence.EventStore;
using Compze.Testing;
using NUnit.Framework;

namespace Compze.Tests.Unit.CQRS.EventHandling;

[TestFixture]public class MutableEventDispatcher_WrappedEventsTests : UniversalTestBase
{
   interface IUserWrapperEvent<out TEvent> : IWrapperEvent<TEvent> where TEvent : IUserEvent;
   class UserWrapperEvent<TEvent>(TEvent @event) : WrapperEvent<TEvent>(@event), IUserWrapperEvent<TEvent>
      where TEvent : IUserEvent;

   interface IUserEvent : IAggregateEvent;
   interface IUserCreatedEvent : IUserEvent;
   class UserCreatedEvent : AggregateEvent, IUserCreatedEvent;


   interface IAdminUserWrapperEvent<out TEvent> : IUserWrapperEvent<TEvent> where TEvent : IUserEvent;
   class AdminUserWrapperEvent<TEvent>(TEvent @event) : UserWrapperEvent<TEvent>(@event), IAdminUserWrapperEvent<TEvent>
      where TEvent : IUserEvent;

   interface IAdminUserEvent : IUserEvent;
   interface IAdminUserCreatedEvent : IAdminUserEvent, IUserCreatedEvent;
   class AdminUserCreatedEvent : AggregateEvent, IAdminUserCreatedEvent;


   IMutableEventDispatcher<IEvent> _dispatcher;
   [SetUp] public void SetupTask() => _dispatcher = IMutableEventDispatcher<IEvent>.New();

   public class Publishing_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
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

   public class Publishing_WrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new WrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

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

   public class Publishing_UserWrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
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

   public class Publishing_AdminUserWrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
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

   public class Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
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