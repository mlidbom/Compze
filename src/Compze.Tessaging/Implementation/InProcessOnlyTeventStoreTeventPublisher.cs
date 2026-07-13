using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

public static class InProcessOnlyTeventStoreTeventPublisherRegistrar
{
   public static IComponentRegistrar InProcessOnlyTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.InProcessOnlyTeventStoreTeventPublisher.RegisterWith);
}

///<summary>The in-process-only <see cref="ITeventStoreTeventPublisher"/>: a taggregate's committed tevents are delivered only to this process's handlers, via <see cref="IInProcessTeventPublisher"/>.</summary>
[UsedImplicitly] class InProcessOnlyTeventStoreTeventPublisher(IInProcessTeventPublisher inProcessTeventPublisher) : ITeventStoreTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((IInProcessTeventPublisher inProcessTeventPublisher)
                                                   => new InProcessOnlyTeventStoreTeventPublisher(inProcessTeventPublisher)));

   readonly IInProcessTeventPublisher _inProcessTeventPublisher = inProcessTeventPublisher;

   void ITeventStoreTeventPublisher.Publish(ITaggregateIdentifyingTevent<ITaggregateTevent> wrappedTevent, IScopeResolver scopeResolver)
      => _inProcessTeventPublisher.Publish(wrappedTevent, scopeResolver);
}
