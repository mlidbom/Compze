using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Tests.Infrastructure;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

public class MutableTeventDispatcher_WrappedTeventsTests : UniversalTestBase
{
   readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New(TeventDispatcherConfig.IgnoreAllUnhandled); //These specifications assert routing, not unhandled-tevent validation, so no tevent is required to have a matching handler.

   public class Publishing_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertUserCreatedTevent() => _dispatcher.Assert().Tevent(new UserCreatedTevent());

      public class Dispatches_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTevent<ITevent>>();
         [XF] public void IPublisherTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserTevent>>();
         [XF] public void IPublisherTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertUserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertUserCreatedTevent().DoesNotDispatchTo<IAdminUserCreatedTevent>();
         [XF] public void IUserPublisherTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherTevent<IUserTevent>>();
      }
   }

   public class Publishing_PublisherTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new PublisherTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_PublisherTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTevent<ITevent>>();
         [XF] public void IPublisherTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserTevent>>();
         [XF] public void IPublisherTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_PublisherTevent_of_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertUserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void IUserPublisherTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherTevent<IUserTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherTevent<IUserTevent>>();
      }
   }

   public class Publishing_UserPublisherTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertUserPublisherTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new UserPublisherTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_UserPublisherTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherTevent_of_ITevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTevent<ITevent>>();
         [XF] public void IPublisherTevent_of_IUserTevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserTevent>>();
         [XF] public void IPublisherTevent_of_IUserCreatedTevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserCreatedTevent>>();
         [XF] public void IUserPublisherTevent_of_IUserTevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTevent<IUserTevent>>();
         [XF] public void IUserPublisherTevent_of_IUserCreatedTevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserPublisherTevent_of_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertUserPublisherTevent_of_UserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void _IAdminUserPublisherTevent_of_IUserTevent() => AssertUserPublisherTevent_of_UserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherTevent<IUserTevent>>();
      }
   }

   public class Publishing_AdminUserPublisherTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertAdminUserPublisherTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserPublisherTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherTevent_of_ITevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTevent<ITevent>>();
         [XF] public void IPublisherTevent_of_IUserTevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserTevent>>();
         [XF] public void IPublisherTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserCreatedTevent>>();
         [XF] public void IUserPublisherTevent_of_IUserTevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTevent<IUserTevent>>();
         [XF] public void IUserPublisherTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IUserTevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTevent<IUserTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_AdminUserPublisherTevent_of_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserPublisherTevent_of_UserCreatedTevent().DoesNotDispatchTo<IAdminUserCreatedTevent>();
      }
   }

   public class Publishing_AdminUserPublisherTevent_of_AdminUserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherTevent<AdminUserCreatedTevent>(new AdminUserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserPublisherTevent_of_AdminUserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IAdminUserTevent_() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserCreatedTevent>();
         [XF] public void IPublisherTevent_of_ITevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTevent<ITevent>>();
         [XF] public void IPublisherTevent_of_IUserTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserTevent>>();
         [XF] public void IPublisherTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherTevent<IUserCreatedTevent>>();
         [XF] public void IUserPublisherTevent_of_IUserTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherTevent<IUserTevent>>();
         [XF] public void IUserPublisherTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IUserTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTevent<IUserTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IAdminUserTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTevent<IAdminUserTevent>>();
         [XF] public void IAdminUserPublisherTevent_of_IAdminUserCreatedTevent() => AssertAdminUserPublisherTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTevent<IAdminUserCreatedTevent>>();
      }
   }

   interface IUserPublisherTevent<out TTevent> : IPublisherTevent<TTevent> where TTevent : IUserTevent;
   class UserPublisherTevent<TTevent>(TTevent tevent) : PublisherTevent<TTevent>(tevent), IUserPublisherTevent<TTevent>
      where TTevent : IUserTevent;

   interface IUserTevent : ITaggregateTevent;
   interface IUserCreatedTevent : IUserTevent;
   class UserCreatedTevent : TaggregateTevent, IUserCreatedTevent;


   interface IAdminUserPublisherTevent<out TTevent> : IUserPublisherTevent<TTevent> where TTevent : IUserTevent;
   class AdminUserPublisherTevent<TTevent>(TTevent tevent) : UserPublisherTevent<TTevent>(tevent), IAdminUserPublisherTevent<TTevent>
      where TTevent : IUserTevent;

   interface IAdminUserTevent : IUserTevent;
   interface IAdminUserCreatedTevent : IAdminUserTevent, IUserCreatedTevent;
   class AdminUserCreatedTevent : TaggregateTevent, IAdminUserCreatedTevent;
}
