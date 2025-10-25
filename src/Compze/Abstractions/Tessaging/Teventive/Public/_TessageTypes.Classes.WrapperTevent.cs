using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

static class WrapperTevent
{
   public static IWrapperTevent<ITevent> WrapTevent(ITevent theTevent) =>
      WrapperTeventImplementationGenerator.ConstructorFor(theTevent.GetType()).Invoke(theTevent);
}

public class WrapperTevent<TTeventInterface>(TTeventInterface @tevent) : IWrapperTevent<TTeventInterface>
   where TTeventInterface : ITevent
{
   public TTeventInterface Tevent { get; } = @tevent;
}
