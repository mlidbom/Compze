using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

static class WrapperEvent
{
   public static IWrapperTevent<ITevent> WrapEvent(ITevent theTevent) =>
      WrapperEventImplementationGenerator.ConstructorFor(theTevent.GetType()).Invoke(theTevent);
}

public class WrapperTevent<TEventInterface>(TEventInterface @event) : IWrapperTevent<TEventInterface>
   where TEventInterface : ITevent
{
   public TEventInterface Event { get; } = @event;
}
