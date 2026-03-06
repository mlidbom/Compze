using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure;

namespace Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;

static class PublisherTypeIdentifyingTevent
{
   public static IPublisherIdentifyingTevent<ITevent> WrapTevent(ITevent theTevent) =>
      WrapperTeventImplementationGenerator.ConstructorFor(theTevent.GetType()).Invoke(theTevent);
}

public class PublisherIdentifyingTevent<TTeventInterface>(TTeventInterface tevent) : IPublisherIdentifyingTevent<TTeventInterface>
   where TTeventInterface : ITevent
{
   public TTeventInterface Tevent { get; } = tevent;
}
