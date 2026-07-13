using Compze.Abstractions.Tessaging.Validation;
using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class DistributedTeventStoreTeventPublisherRegistrar
{
   public static IComponentRegistrar DistributedTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.DistributedTeventStoreTeventPublisher.RegisterWith);
}

///<summary>The distributed <see cref="ITeventStoreTeventPublisher"/>: a taggregate's committed tevents are delivered both to this process's handlers (via <see cref="IInProcessTeventPublisher"/>) and, through the <see cref="IOutbox"/>, to subscribers on other endpoints.</summary>
[UsedImplicitly] class DistributedTeventStoreTeventPublisher(IOutbox outbox, IInProcessTeventPublisher inProcessTeventPublisher) : ITeventStoreTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((IOutbox outbox, IInProcessTeventPublisher inProcessTeventPublisher)
                                                   => new DistributedTeventStoreTeventPublisher(outbox, inProcessTeventPublisher)));

   readonly IOutbox _outbox = outbox;
   readonly IInProcessTeventPublisher _inProcessTeventPublisher = inProcessTeventPublisher;

   void ITeventStoreTeventPublisher.Publish(ITaggregateIdentifyingTevent<ITaggregateTevent> wrappedTevent, IScopeResolver scopeResolver)
   {
      TessageInspector.AssertValidToSendRemote(wrappedTevent.Tevent);
      _inProcessTeventPublisher.Publish(wrappedTevent, scopeResolver);
      _outbox.PublishTransactionally(wrappedTevent);
   }
}
