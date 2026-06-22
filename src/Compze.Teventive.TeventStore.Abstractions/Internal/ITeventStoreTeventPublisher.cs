using Compze.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Teventive.TeventStore.Internal;

public interface ITeventStoreTeventPublisher
{
   void Publish(ITaggregateTevent aTevent, IScopeResolver scopeResolver);
}
