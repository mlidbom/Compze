using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

static class WrapperTevent
{
   public static IPublisherTypeIdentifyingTevent<ITevent> WrapTevent(ITevent theTevent) =>
      WrapperTeventImplementationGenerator.ConstructorFor(theTevent.GetType()).Invoke(theTevent);
}

public class PublisherTypeIdentifyingTevent<TTeventInterface>(TTeventInterface tevent) : IPublisherTypeIdentifyingTevent<TTeventInterface>
   where TTeventInterface : ITevent
{
   public TTeventInterface Tevent { get; } = tevent;
}
