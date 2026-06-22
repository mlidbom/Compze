using Compze.Abstractions.Tessaging.Public;
using Compze.Tessaging.Teventive.Public;
using Compze.Must;

namespace Compze.Tests.Unit.CQRS.TeventHandling;

static class TeventDispatcherAsserter
{
   internal class DispatcherAssertion<TDispatcherRootTevent>(IMutableTeventDispatcher<TDispatcherRootTevent> dispatcher)
      where TDispatcherRootTevent : class, ITevent
   {
      readonly IMutableTeventDispatcher<TDispatcherRootTevent> _dispatcher = dispatcher;

      public WrappedRouteAssertion<TDispatcherRootTevent> WrappedTevent<TPublishedTevent>(TPublishedTevent tevent) where TPublishedTevent : IPublisherIdentifyingTevent<TDispatcherRootTevent> => new(_dispatcher, tevent);
      public RouteAssertion<TDispatcherRootTevent> Tevent<TPublishedTevent>(TPublishedTevent tevent) where TPublishedTevent : TDispatcherRootTevent => new(_dispatcher, tevent);
   }

   internal class RouteAssertion<TDispatcherRootTevent>(IMutableTeventDispatcher<TDispatcherRootTevent> dispatcher, TDispatcherRootTevent tevent)
      where TDispatcherRootTevent : class, ITevent
   {
      readonly IMutableTeventDispatcher<TDispatcherRootTevent> _dispatcher = dispatcher;
      readonly TDispatcherRootTevent _tevent = tevent;

      public void DispatchesTo<THandlerTevent>()
         where THandlerTevent : TDispatcherRootTevent
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().For((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToGeneric<THandlerTevent>()
         where THandlerTevent : ITevent
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForGenericTevent((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToWrapped<THandlerTevent>()
         where THandlerTevent : IPublisherIdentifyingTevent<TDispatcherRootTevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToWrappedGeneric<THandlerTevent>()
         where THandlerTevent : IPublisherIdentifyingTevent<ITevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrappedGeneric((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DoesNotDispatchToWrapped<THandlerTevent>()
         where THandlerTevent : IPublisherIdentifyingTevent<TDispatcherRootTevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(0, "Tessage was dispatched to handler.");
      }
   }

   internal class WrappedRouteAssertion<TDispatcherRootTevent>(IMutableTeventDispatcher<TDispatcherRootTevent> dispatcher, IPublisherIdentifyingTevent<TDispatcherRootTevent> tevent)
      where TDispatcherRootTevent : class, ITevent
   {
      readonly IMutableTeventDispatcher<TDispatcherRootTevent> _dispatcher = dispatcher;
      readonly IPublisherIdentifyingTevent<TDispatcherRootTevent> _tevent = tevent;

      public void DispatchesTo<THandlerTevent>()
         where THandlerTevent : TDispatcherRootTevent
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().For((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToGeneric<THandlerTevent>()
         where THandlerTevent : ITevent
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForGenericTevent((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToWrapped<THandlerTevent>()
         where THandlerTevent : IPublisherIdentifyingTevent<TDispatcherRootTevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DispatchesToWrappedGeneric<THandlerTevent>()
         where THandlerTevent : IPublisherIdentifyingTevent<ITevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrappedGeneric((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(1, "Tessage was not dispatched to handler.");
      }

      public void DoesNotDispatchToWrapped<THandlerTevent>()
         where THandlerTevent : IPublisherIdentifyingTevent<TDispatcherRootTevent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerTevent _) => callCount++);
         _dispatcher.Dispatch(_tevent);
         callCount.Must().Be(0, "Tessage was dispatched to handler.");
      }
   }

   internal static DispatcherAssertion<TTevent> Assert<TTevent>(this IMutableTeventDispatcher<TTevent> @this) where TTevent : class, ITevent => new(@this);
}
