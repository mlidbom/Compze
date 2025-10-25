using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

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
