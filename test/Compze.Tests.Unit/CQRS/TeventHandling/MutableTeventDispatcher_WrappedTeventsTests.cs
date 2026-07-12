using Compze.Abstractions.Tessaging.Public;
using Compze.Tests.Infrastructure;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.Teventive.Tevents.Public;
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
         [XF] public void IPublisherIdentifyingTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertUserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertUserCreatedTevent().DoesNotDispatchTo<IAdminUserCreatedTevent>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_PublisherIdentifyingTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new PublisherIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_PublisherIdentifyingTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherIdentifyingTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_PublisherIdentifyingTevent_of_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertUserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_UserPublisherIdentifyingTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new UserPublisherIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_UserPublisherIdentifyingTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherIdentifyingTevent_of_ITevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserTevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserTevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserPublisherIdentifyingTevent_of_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void _IAdminUserPublisherIdentifyingTevent_of_IUserTevent() => AssertUserPublisherIdentifyingTevent_of_UserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_AdminUserPublisherIdentifyingTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserPublisherIdentifyingTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IPublisherIdentifyingTevent_of_ITevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_AdminUserPublisherIdentifyingTevent_of_UserCreatedTevent
      {
         [XF] public void IAdminUserTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DoesNotDispatchTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_UserCreatedTevent().DoesNotDispatchTo<IAdminUserCreatedTevent>();
      }
   }

   public class Publishing_AdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherIdentifyingTevent<AdminUserCreatedTevent>(new AdminUserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IAdminUserTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserCreatedTevent>();
         [XF] public void IPublisherIdentifyingTevent_of_ITevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IAdminUserTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IAdminUserTevent>>();
         [XF] public void IAdminUserPublisherIdentifyingTevent_of_IAdminUserCreatedTevent() => AssertAdminUserPublisherIdentifyingTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IAdminUserCreatedTevent>>();
      }
   }

   interface IUserPublisherIdentifyingTevent<out TTevent> : IPublisherIdentifyingTevent<TTevent> where TTevent : IUserTevent;
   class UserPublisherIdentifyingTevent<TTevent>(TTevent tevent) : PublisherIdentifyingTevent<TTevent>(tevent), IUserPublisherIdentifyingTevent<TTevent>
      where TTevent : IUserTevent;

   interface IUserTevent : ITaggregateTevent;
   interface IUserCreatedTevent : IUserTevent;
   class UserCreatedTevent : TaggregateTevent, IUserCreatedTevent;


   interface IAdminUserPublisherIdentifyingTevent<out TTevent> : IUserPublisherIdentifyingTevent<TTevent> where TTevent : IUserTevent;
   class AdminUserPublisherIdentifyingTevent<TTevent>(TTevent tevent) : UserPublisherIdentifyingTevent<TTevent>(tevent), IAdminUserPublisherIdentifyingTevent<TTevent>
      where TTevent : IUserTevent;

   interface IAdminUserTevent : IUserTevent;
   interface IAdminUserCreatedTevent : IAdminUserTevent, IUserCreatedTevent;
   class AdminUserCreatedTevent : TaggregateTevent, IAdminUserCreatedTevent;
}
