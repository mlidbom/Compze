using Compze.Must;

using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.xUnitBDD;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE0051 //Reviewed OK: unused private members are intentional in this test.
#pragma warning disable IDE1006 //Reviewed OK: Test Naming Styles

namespace Compze.Tests.Unit.CQRS.TeventHandling;

public static class MutableTeventDispatcher_specification
{
   public class Given_an_instance
   {
      readonly IMutableTeventDispatcher<IUserTevent> _dispatcher;

      protected Given_an_instance(TeventDispatcherConfig? dispatcherConfig = null) => _dispatcher = IMutableTeventDispatcher<IUserTevent>.New(dispatcherConfig);

      public class with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_tevent_type : Given_an_instance
      {
         int CallsMade { get; set; }

         int? BeforeHandlers1CallOrder { get; set; }
         int? BeforeHandlers2CallOrder { get; set; }

         int? UserCreatedCallOrder { get; set; }

         int? AfterHandlers1CallOrder { get; set; }
         int? AfterHandlers2CallOrder { get; set; }

         public with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_tevent_type()
            : base(TeventDispatcherConfig.Default.IgnoreUnhandled<IUserTevent.IIgnoredUserTevent>())
         {
            _dispatcher.Register()
                       .BeforeHandlers(_ => BeforeHandlers1CallOrder = ++CallsMade)
                       .BeforeHandlers(_ => BeforeHandlers2CallOrder = ++CallsMade)
                       .AfterHandlers(_ => AfterHandlers1CallOrder = ++CallsMade)
                       .AfterHandlers(_ => AfterHandlers2CallOrder = ++CallsMade)
                       .For<IUserTevent.IUserCreatedTevent>(_ => UserCreatedCallOrder = ++CallsMade)
                       .For<IUserTevent.IUserRegistered>(_ => ++CallsMade)
                       .For<IUserTevent.IUserSkillsRemoved>(_ => ++CallsMade)
                       .For<IUserTevent.IUserSkillsAdded>(_ => ++CallsMade);
         }

         [XF] public void when_dispatching_an_ignored_tevent_no_calls_are_made_to_any_handlers()
         {
            _dispatcher.Dispatch(new IgnoredUserTevent());
            CallsMade.Must().Be(0);
         }

         [XF] public void when_dispatching_an_unhandled_tevent_that_is_not_ignored_an_exception_is_thrown() =>
            _dispatcher.Invoking(it => it.Dispatch(new UnHandledUserTevent())).Must().Throw<TeventUnhandledException>();

         public class when_dispatching_an_IUserCreatedTevent : with_2_BeforeHandlers_2_AfterHandlers_and_1_handler_each_per_4_specific_tevent_type
         {
            public when_dispatching_an_IUserCreatedTevent() => _dispatcher.Dispatch(new UserCreatedTevent());

            [XF] public void BeforeHandler1_is_called_first() => BeforeHandlers1CallOrder.Must().Be(1);
            [XF] public void BeforeHandler2_is_called_second() => BeforeHandlers2CallOrder.Must().Be(2);
            [XF] public void The_specific_handler_is_called_third() => UserCreatedCallOrder.Must().Be(3);
            [XF] public void AfterHandler1_is_called_fourth() => AfterHandlers1CallOrder.Must().Be(4);
            [XF] public void AfterHandler2_is_called_fifth() => AfterHandlers2CallOrder.Must().Be(5);
            [XF] public void Five_calls_are_made_in_total() => CallsMade.Must().Be(5);
         }
      }

      public class with_2_registered_handlers_for_the_same_tevent_type_then_when_dispatching_tevent : Given_an_instance
      {
         [XF] public void handlers_are_called_in_registration_order()
         {
            var calls = 0;
            var handler1CallOrder = 0;
            var handler2CallOrder = 0;

            _dispatcher.Register()
                       .For<IUserTevent.IUserRegistered>(_ => handler1CallOrder = ++calls)
                       .For<IUserTevent.IUserRegistered>(_ => handler2CallOrder = ++calls);

            _dispatcher.Dispatch(new UserRegistered());

            handler1CallOrder.Must().Be(1);
            handler2CallOrder.Must().Be(2);
         }
      }

      interface IUserTevent : ITaggregateTevent
      {
         interface IUserCreatedTevent : IUserTevent;
         interface IUserRegistered : IUserCreatedTevent;
         interface IUserSkillsTevent : IUserTevent;
         interface IUserSkillsAdded : IUserSkillsTevent;
         interface IUserSkillsRemoved : IUserSkillsTevent;
         interface IIgnoredUserTevent : IUserTevent;
      }

      class UnHandledUserTevent : TaggregateTevent, IUserTevent;

      class IgnoredUserTevent : TaggregateTevent, IUserTevent.IIgnoredUserTevent;

      class UserCreatedTevent : TaggregateTevent, IUserTevent.IUserCreatedTevent;

      class UserRegistered : TaggregateTevent, IUserTevent.IUserRegistered;
   }
}
