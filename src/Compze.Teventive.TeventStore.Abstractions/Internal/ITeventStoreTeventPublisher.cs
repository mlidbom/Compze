using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Public.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Internal;

public interface ITeventStoreTeventPublisher
{
   void Publish(ITaggregateTevent aTevent, IScopeResolver scopeResolver);
}
