using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;

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
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTypeIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherTypeIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_WrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new PublisherTypeIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_WrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTypeIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_WrapperTevent_of_UserCreatedTevent
      {
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherTypeIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_UserWrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertUserWrapperTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new UserPublisherTypeIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_UserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTypeIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void _IAdminUserWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserPublisherTypeIdentifyingTevent<IUserTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertAdminUserWrapperTevent_of_UserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherTypeIdentifyingTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTypeIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperTevent_of_AdminUserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.WrappedRouteAssertion<IUserTevent> AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent() => _dispatcher.Assert().WrappedTevent(new AdminUserPublisherTypeIdentifyingTevent<AdminUserCreatedTevent>(new AdminUserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperTevent_of_AdminUserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToGeneric<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IAdminUserTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrappedGeneric<IPublisherTypeIdentifyingTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTypeIdentifyingTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IAdminUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserPublisherTypeIdentifyingTevent<IAdminUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IAdminUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserPublisherTypeIdentifyingTevent<IAdminUserCreatedTevent>>();
      }
   }

   interface IUserPublisherTypeIdentifyingTevent<out TTevent> : IPublisherTypeIdentifyingTevent<TTevent> where TTevent : IUserTevent;
   class UserPublisherTypeIdentifyingTevent<TTevent>(TTevent tevent) : PublisherTypeIdentifyingTevent<TTevent>(tevent), IUserPublisherTypeIdentifyingTevent<TTevent>
      where TTevent : IUserTevent;

   interface IUserTevent : ITaggregateTevent;
   interface IUserCreatedTevent : IUserTevent;
   class UserCreatedTevent : TaggregateTevent, IUserCreatedTevent;


   interface IAdminUserPublisherTypeIdentifyingTevent<out TTevent> : IUserPublisherTypeIdentifyingTevent<TTevent> where TTevent : IUserTevent;
   class AdminUserPublisherTypeIdentifyingTevent<TTevent>(TTevent tevent) : UserPublisherTypeIdentifyingTevent<TTevent>(tevent), IAdminUserPublisherTypeIdentifyingTevent<TTevent>
      where TTevent : IUserTevent;

   interface IAdminUserTevent : IUserTevent;
   interface IAdminUserCreatedTevent : IAdminUserTevent, IUserCreatedTevent;
   class AdminUserCreatedTevent : TaggregateTevent, IAdminUserCreatedTevent;
}
