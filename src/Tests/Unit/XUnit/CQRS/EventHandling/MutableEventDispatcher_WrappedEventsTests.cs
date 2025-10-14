using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tessaging.Common.Teventive;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tests.Infrastructure;
using Xunit;
using Compze.Tests.Infrastructure.XUnit;
using Compze.Tests.Infrastructure.XUnit.TestFrameworkExtensions;

namespace Compze.Tests.Unit.XUnit.CQRS.EventHandling;

public class MutableEventDispatcher_WrappedEventsTests : XUnitTestBase
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
   
   public MutableEventDispatcher_WrappedEventsTests() => _dispatcher = IMutableEventDispatcher<IEvent>.New();

   public class Publishing_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new UserCreatedEvent());

      public class Dispatches_to_handler_for : Publishing_UserCreatedEvent
      {
         [XFact] public void IEvent() => AssertUserCreatedEvent().DispatchesTo<IEvent>();
         [XFact] public void IUserEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserEvent>();
         [XFact] public void IUserCreatedEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [XFact] public void IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [XFact] public void IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [XFact] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedEvent
      {
         [XFact] public void IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperEvent<IUserEvent>>();
      }
   }

   public class Publishing_WrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new WrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
      {
         [XFact] public void IEvent() => AssertUserCreatedEvent().DispatchesTo<IEvent>();
         [XFact] public void IUserEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserEvent>();
         [XFact] public void IUserCreatedEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [XFact] public void IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [XFact] public void IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [XFact] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
      {
         [XFact] public void IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperEvent<IUserEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
      }
   }

   public class Publishing_UserWrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new UserWrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
      {
         [XFact] public void IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IEvent>();
         [XFact] public void IUserEvent_() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserEvent>();
         [XFact] public void IUserCreatedEvent_() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [XFact] public void IWrapperEvent_of_IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [XFact] public void IWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [XFact] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
         [XFact] public void IUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
         [XFact] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
      {
         [XFact] public void _IAdminUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
      }
   }

   public class Publishing_AdminUserWrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertAdminUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new AdminUserWrapperEvent<UserCreatedEvent>(new UserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperEvent_of_UserCreatedEvent
      {
         [XFact] public void IEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IEvent>();
         [XFact] public void IUserEvent_() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserEvent>();
         [XFact] public void IUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [XFact] public void IWrapperEvent_of_IEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [XFact] public void IWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [XFact] public void IWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
         [XFact] public void IUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
         [XFact] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
      }
   }

   public class Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<IEvent> AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent() => _dispatcher.Assert().Event(new AdminUserWrapperEvent<AdminUserCreatedEvent>(new AdminUserCreatedEvent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent
      {
         [XFact] public void IEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IEvent>();
         [XFact] public void IUserEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IUserEvent>();
         [XFact] public void IUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IUserCreatedEvent>();
         [XFact] public void IAdminUserEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IAdminUserEvent>();
         [XFact] public void IAdminUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IAdminUserCreatedEvent>();
         [XFact] public void IWrapperEvent_of_IEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IEvent>>();
         [XFact] public void IWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserEvent>>();
         [XFact] public void IWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperEvent<IUserCreatedEvent>>();
         [XFact] public void IUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserEvent>>();
         [XFact] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperEvent<IUserEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IUserCreatedEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IAdminUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperEvent<IAdminUserEvent>>();
         [XFact] public void IAdminUserWrapperEvent_of_IAdminUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperEvent<IAdminUserCreatedEvent>>();
      }
   }
}
