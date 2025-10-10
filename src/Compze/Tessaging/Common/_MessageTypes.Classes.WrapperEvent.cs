using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Common;

static class WrapperEvent
{
   public static IWrapperEvent<IEvent> WrapEvent(IEvent theEvent) =>
      WrapperEventImplementationGenerator.ConstructorFor(theEvent.GetType()).Invoke(theEvent);
}

public class WrapperEvent<TEventInterface>(TEventInterface @event) : IWrapperEvent<TEventInterface>
   where TEventInterface : IEvent
{
   public TEventInterface Event { get; } = @event;
}
