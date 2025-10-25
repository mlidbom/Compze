using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.EventStore.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.EventHandling;

public class MutableEventDispatcher_WrappedEventsTests : UniversalTestBase
{
   interface IUserWrapperTevent<out TEvent> : IWrapperTevent<TEvent> where TEvent : IUserTevent;
   class UserWrapperTevent<TEvent>(TEvent @event) : WrapperTevent<TEvent>(@event), IUserWrapperTevent<TEvent>
      where TEvent : IUserTevent;

   interface IUserTevent : IAggregateTevent;
   interface IUserCreatedTevent : IUserTevent;
   class UserCreatedTevent : AggregateTevent, IUserCreatedTevent;


   interface IAdminUserWrapperTevent<out TEvent> : IUserWrapperTevent<TEvent> where TEvent : IUserTevent;
   class AdminUserWrapperTevent<TEvent>(TEvent @event) : UserWrapperTevent<TEvent>(@event), IAdminUserWrapperTevent<TEvent>
      where TEvent : IUserTevent;

   interface IAdminUserTevent : IUserTevent;
   interface IAdminUserCreatedTevent : IAdminUserTevent, IUserCreatedTevent;
   class AdminUserCreatedTevent : AggregateTevent, IAdminUserCreatedTevent;


   IMutableEventDispatcher<ITevent> _dispatcher;
   
   public MutableEventDispatcher_WrappedEventsTests() => _dispatcher = IMutableEventDispatcher<ITevent>.New();

   public class Publishing_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<ITevent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new UserCreatedTevent());

      public class Dispatches_to_handler_for : Publishing_UserCreatedEvent
      {
         [XF] public void IEvent() => AssertUserCreatedEvent().DispatchesTo<ITevent>();
         [XF] public void IUserEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedEvent
      {
         [XF] public void IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperTevent<IUserTevent>>();
      }
   }

   public class Publishing_WrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<ITevent> AssertUserCreatedEvent() => _dispatcher.Assert().Event(new WrapperTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
      {
         [XF] public void IEvent() => AssertUserCreatedEvent().DispatchesTo<ITevent>();
         [XF] public void IUserEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedEvent_() => AssertUserCreatedEvent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperEvent_of_IEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_WrapperEvent_of_UserCreatedEvent
      {
         [XF] public void IUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertUserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
      }
   }

   public class Publishing_UserWrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<ITevent> AssertUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new UserWrapperTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
      {
         [XF] public void IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<ITevent>();
         [XF] public void IUserEvent_() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedEvent_() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperEvent_of_IEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperEvent_of_UserCreatedEvent
      {
         [XF] public void _IAdminUserWrapperEvent_of_IUserEvent() => AssertUserWrapperEvent_of_UserCreatedEvent().DoesNotDispatchToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperEvent_of_UserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<ITevent> AssertAdminUserWrapperEvent_of_UserCreatedEvent() => _dispatcher.Assert().Event(new AdminUserWrapperTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperEvent_of_UserCreatedEvent
      {
         [XF] public void IEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<ITevent>();
         [XF] public void IUserEvent_() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperEvent_of_IEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_UserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent : MutableEventDispatcher_WrappedEventsTests
   {
      EventDispatcherAsserter.RouteAssertion<ITevent> AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent() => _dispatcher.Assert().Event(new AdminUserWrapperTevent<AdminUserCreatedTevent>(new AdminUserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperEvent_of_AdminUserCreatedEvent
      {
         [XF] public void IEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<ITevent>();
         [XF] public void IUserEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IAdminUserEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedEvent_() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesTo<IAdminUserCreatedTevent>();
         [XF] public void IWrapperEvent_of_IEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IAdminUserEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IAdminUserWrapperTevent<IAdminUserTevent>>();
         [XF] public void IAdminUserWrapperEvent_of_IAdminUserCreatedEvent() => AssertAdminUserWrapperEvent_of_AdminUserCreatedEvent().DispatchesToWrapped<IUserWrapperTevent<IAdminUserCreatedTevent>>();
      }
   }
}
