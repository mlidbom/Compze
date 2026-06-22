using Compze.Abstractions.Tessaging.Public;

namespace Compze.Teventive.Public;

public interface ITeventDispatcher<in TTevent>
   where TTevent : ITevent
{
   void Dispatch(TTevent evt);
   void Dispatch(IPublisherIdentifyingTevent<TTevent> evt);
}
