using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Internal;

public interface ITeventStoreTeventPublisher
{
   ///<summary>Publishes a committed tevent onward, exactly as its taggregate published it: inside its publisher's <see cref="ITaggregateIdentifyingTevent{TTeventInterface}"/> wrapper.</summary>
   void Publish(ITaggregateIdentifyingTevent<ITaggregateTevent> wrappedTevent, IScopeResolver scopeResolver);
}
