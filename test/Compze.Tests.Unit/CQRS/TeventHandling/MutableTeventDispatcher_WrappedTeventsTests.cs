using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Teventive.Public;
using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure;
using Compze.xUnitBDD;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

public class MutableTeventDispatcher_WrappedTeventsTests : UniversalTestBase
{
   readonly IMutableTeventDispatcher<IUserTevent> _dispatcher = IMutableTeventDispatcher<IUserTevent>.New();

   public class Publishing_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<IUserTevent> AssertUserCreatedTevent() => _dispatcher.Assert().Tevent(new UserCreatedTevent());

      public class Dispatches_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_WrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new PublisherIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_WrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_WrapperTevent_of_UserCreatedTevent
      {
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_UserWrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertUserWrapperTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new UserPublisherIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_UserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void _IAdminUserWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertAdminUserWrapperTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperTevent_of_AdminUserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherIdentifyingTevent<AdminUserCreatedTevent>(new AdminUserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperTevent_of_AdminUserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IAdminUserTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IAdminUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherIdentifyingTevent<IAdminUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IAdminUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherIdentifyingTevent<IAdminUserCreatedTevent>>();
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
