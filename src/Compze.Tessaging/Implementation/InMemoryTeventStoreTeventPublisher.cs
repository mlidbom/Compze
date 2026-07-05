using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

public static class InMemoryTeventStoreTeventPublisherRegistrar
{
   public static IComponentRegistrar InMemoryTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.InMemoryTeventStoreTeventPublisher.RegisterWith);
}

///<summary>The no-bus <see cref="ITeventStoreTeventPublisher"/>: a taggregate's committed tevents are delivered only to this process's handlers, via <see cref="IInProcessTeventPublisher"/>.</summary>
[UsedImplicitly] class InMemoryTeventStoreTeventPublisher(IInProcessTeventPublisher inProcessTeventPublisher) : ITeventStoreTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((IInProcessTeventPublisher inProcessTeventPublisher)
                                                   => new InMemoryTeventStoreTeventPublisher(inProcessTeventPublisher)));

   readonly IInProcessTeventPublisher _inProcessTeventPublisher = inProcessTeventPublisher;

   void ITeventStoreTeventPublisher.Publish(ITaggregateTevent tevent, IScopeResolver scopeResolver)
      => _inProcessTeventPublisher.Publish(tevent, scopeResolver);
}
