using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using FluentAssertions;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

static class TeventDispatcherAsserter
{
   internal class DispatcherAssertion<TDispatcherRootTevent>(IMutableTeventDispatcher<TDispatcherRootTevent> dispatcher)
      where TDispatcherRootTevent : class, ITevent
   {
      readonly IMutableTeventDispatcher<TDispatcherRootTevent> _dispatcher = dispatcher;

      public RouteAssertion<TDispatcherRootTevent> Tevent<TPublishedTevent>(TPublishedTevent @tevent) where TPublishedTevent : TDispatcherRootTevent => new(_dispatcher, @tevent);
   }

   internal class RouteAssertion<TDispatcherRootTevent>(IMutableTeventDispatcher<TDispatcherRootTevent> dispatcher, TDispatcherRootTevent @tevent)
      where TDispatcherRootTevent : class, ITevent
   {
      readonly IMutableTeventDispatcher<TDispatcherRootTevent> _dispatcher = dispatcher;
      readonly TDispatcherRootTevent _tevent = @tevent;

      public void DispatchesTo<THandlerTevent>()
         where THandlerTevent : TDispatcherRootTevent
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().For((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Should().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToWrapped<THandlerTevent>()
         where THandlerTevent : IWrapperTevent<TDispatcherRootTevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Should().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DoesNotDispatchToWrapped<THandlerTevent>()
         where THandlerTevent : IWrapperTevent<TDispatcherRootTevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Should().Be(0, "Tessage was dispatched to handler.");
      }
   }

   internal static DispatcherAssertion<TTevent> Assert<TTevent>(this IMutableTeventDispatcher<TTevent> @this) where TTevent : class, ITevent => new(@this);
}