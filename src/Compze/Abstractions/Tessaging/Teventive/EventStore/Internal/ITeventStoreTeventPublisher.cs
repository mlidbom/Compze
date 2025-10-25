using Compze.Abstractions.Tessaging.Teventive.Public;

namespace Compze.Abstractions.Tessaging.Teventive.TeventStore.Internal;

interface ITeventStoreTeventPublisher
{
   void Publish(IAggregateTevent aTevent);
}
