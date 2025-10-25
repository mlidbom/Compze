using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

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
