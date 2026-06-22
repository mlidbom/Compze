using Compze.Abstractions.Tessaging.Public;
using Compze.Teventive.Infrastructure;

namespace Compze.Teventive.Public.Taggregates.Tevents.Public;

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
