using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.XUnit.BDD;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

public class MutableTeventDispatcher_WrappedTeventsTests : UniversalTestBase
{
   interface IUserWrapperTevent<out TTevent> : IWrapperTevent<TTevent> where TTevent : IUserTevent;
   class UserWrapperTevent<TTevent>(TTevent @tevent) : WrapperTevent<TTevent>(@tevent), IUserWrapperTevent<TTevent>
      where TTevent : IUserTevent;

   interface IUserTevent : ITaggregateTevent;
   interface IUserCreatedTevent : IUserTevent;
   class UserCreatedTevent : TaggregateTevent, IUserCreatedTevent;


   interface IAdminUserWrapperTevent<out TTevent> : IUserWrapperTevent<TTevent> where TTevent : IUserTevent;
   class AdminUserWrapperTevent<TTevent>(TTevent @tevent) : UserWrapperTevent<TTevent>(@tevent), IAdminUserWrapperTevent<TTevent>
      where TTevent : IUserTevent;

   interface IAdminUserTevent : IUserTevent;
   interface IAdminUserCreatedTevent : IAdminUserTevent, IUserCreatedTevent;
   class AdminUserCreatedTevent : TaggregateTevent, IAdminUserCreatedTevent;


   IMutableTeventDispatcher<ITevent> _dispatcher;
   
   public MutableTeventDispatcher_WrappedTeventsTests() => _dispatcher = IMutableTeventDispatcher<ITevent>.New();

   public class Publishing_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<ITevent> AssertUserCreatedTevent() => _dispatcher.Assert().Tevent(new UserCreatedTevent());

      public class Dispatches_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesTo<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserCreatedTevent
      {
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserWrapperTevent<IUserTevent>>();
      }
   }

   public class Publishing_WrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<ITevent> AssertUserCreatedTevent() => _dispatcher.Assert().Tevent(new WrapperTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_WrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserCreatedTevent().DispatchesTo<ITevent>();
         [XF] public void IUserTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_WrapperTevent_of_UserCreatedTevent
      {
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertUserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
      }
   }

   public class Publishing_UserWrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<ITevent> AssertUserWrapperTevent_of_UserCreatedTevent() => _dispatcher.Assert().Tevent(new UserWrapperTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_UserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<ITevent>();
         [XF] public void IUserTevent_() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
      }

      public class Does_not_dispatch_to_handler_for : Publishing_UserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void _IAdminUserWrapperTevent_of_IUserTevent() => AssertUserWrapperTevent_of_UserCreatedTevent().DoesNotDispatchToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperTevent_of_UserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<ITevent> AssertAdminUserWrapperTevent_of_UserCreatedTevent() => _dispatcher.Assert().Tevent(new AdminUserWrapperTevent<UserCreatedTevent>(new UserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperTevent_of_UserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_UserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
      }
   }

   public class Publishing_AdminUserWrapperTevent_of_AdminUserCreatedTevent : MutableTeventDispatcher_WrappedTeventsTests
   {
      TeventDispatcherAsserter.RouteAssertion<ITevent> AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent() => _dispatcher.Assert().Tevent(new AdminUserWrapperTevent<AdminUserCreatedTevent>(new AdminUserCreatedTevent()));

      public class Dispatches_to_handler_for : Publishing_AdminUserWrapperTevent_of_AdminUserCreatedTevent
      {
         [XF] public void ITevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<ITevent>();
         [XF] public void IUserTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserTevent>();
         [XF] public void IUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IUserCreatedTevent>();
         [XF] public void IAdminUserTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserTevent>();
         [XF] public void IAdminUserCreatedTevent_() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesTo<IAdminUserCreatedTevent>();
         [XF] public void IWrapperTevent_of_ITevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<ITevent>>();
         [XF] public void IWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserTevent>>();
         [XF] public void IWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserTevent>>();
         [XF] public void IUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserWrapperTevent<IUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IUserCreatedTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IAdminUserTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IAdminUserWrapperTevent<IAdminUserTevent>>();
         [XF] public void IAdminUserWrapperTevent_of_IAdminUserCreatedTevent() => AssertAdminUserWrapperTevent_of_AdminUserCreatedTevent().DispatchesToWrapped<IUserWrapperTevent<IAdminUserCreatedTevent>>();
      }
   }
}
