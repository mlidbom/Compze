using Compze.Abstractions.Tessaging.Validation;
using Compze.Tessaging.Teventive.TeventStore.Internal;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.Taggregates.Tevents.Public;
using JetBrains.Annotations;

namespace Compze.Tessaging.Implementation;

static class ServiceBusTeventStoreTeventPublisherRegistrar
{
   public static IComponentRegistrar ServiceBusTeventStoreTeventPublisher(this IComponentRegistrar registrar)
      => registrar.Register(Implementation.ServiceBusTeventStoreTeventPublisher.RegisterWith);
}

///<summary>The distributed <see cref="ITeventStoreTeventPublisher"/>: a taggregate's committed tevents are delivered both to this process's handlers (via <see cref="IInProcessTeventPublisher"/>) and, through the <see cref="IOutbox"/>, to subscribers on other endpoints.</summary>
[UsedImplicitly] class ServiceBusTeventStoreTeventPublisher(IOutbox outbox, IInProcessTeventPublisher inProcessTeventPublisher) : ITeventStoreTeventPublisher
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITeventStoreTeventPublisher>()
                                     .CreatedBy((IOutbox outbox, IInProcessTeventPublisher inProcessTeventPublisher)
                                                   => new ServiceBusTeventStoreTeventPublisher(outbox, inProcessTeventPublisher)));

   readonly IOutbox _outbox = outbox;
   readonly IInProcessTeventPublisher _inProcessTeventPublisher = inProcessTeventPublisher;

   void ITeventStoreTeventPublisher.Publish(ITaggregateTevent tevent, IScopeResolver scopeResolver)
   {
      TessageInspector.AssertValidToSendRemote(tevent);
      _inProcessTeventPublisher.Publish(tevent, scopeResolver);
      _outbox.PublishTransactionally(tevent);
   }
}
