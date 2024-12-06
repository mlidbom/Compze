using Compze.Messaging;
using Compze.Messaging.Events;
using FluentAssertions;

namespace Compze.Tests.CQRS.EventHandling;

static class EventDispatcherAsserter
{
   internal class DispatcherAssertion<TDispatcherRootEvent>(IMutableEventDispatcher<TDispatcherRootEvent> dispatcher)
      where TDispatcherRootEvent : class, IEvent
   {
      readonly IMutableEventDispatcher<TDispatcherRootEvent> _dispatcher = dispatcher;

      public RouteAssertion<TDispatcherRootEvent> Event<TPublishedEvent>(TPublishedEvent @event) where TPublishedEvent : TDispatcherRootEvent => new(_dispatcher, @event);
   }

   internal class RouteAssertion<TDispatcherRootEvent>(IMutableEventDispatcher<TDispatcherRootEvent> dispatcher, TDispatcherRootEvent @event)
      where TDispatcherRootEvent : class, IEvent
   {
      readonly IMutableEventDispatcher<TDispatcherRootEvent> _dispatcher = dispatcher;
      readonly TDispatcherRootEvent _event = @event;

      public void DispatchesTo<THandlerEvent>()
         where THandlerEvent : TDispatcherRootEvent
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().For((THandlerEvent _) => callCount++);
         _dispatcher.Dispatch(_event);
         callCount.Should().Be(1, "Message was not dispatched to handler.");
      }

      public void DispatchesToWrapped<THandlerEvent>()
         where THandlerEvent : IWrapperEvent<TDispatcherRootEvent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerEvent _) => callCount++);
         _dispatcher.Dispatch(_event);
         callCount.Should().Be(1, "Message was not dispatched to handler.");
      }

      public void DoesNotDispatchToWrapped<THandlerEvent>()
         where THandlerEvent : IWrapperEvent<TDispatcherRootEvent>
      {
         var callCount = 0;
         _dispatcher.Register().IgnoreAllUnhandled();
         _dispatcher.Register().ForWrapped((THandlerEvent _) => callCount++);
         _dispatcher.Dispatch(_event);
         callCount.Should().Be(0, "Message was dispatched to handler.");
      }
   }

   internal static DispatcherAssertion<TEvent> Assert<TEvent>(this IMutableEventDispatcher<TEvent> @this) where TEvent : class, IEvent => new(@this);
}