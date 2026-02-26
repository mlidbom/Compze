using System.Collections.Generic;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public class ExactlyOnceRoutingClient : IExactlyOnceRoutingClient
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<IExactlyOnceRoutingClient>().CreatedBy(
            (InboxConnectionRouter router) => new ExactlyOnceRoutingClient(router)));

   readonly InboxConnectionRouter _inboxConnectionRouter;

   ExactlyOnceRoutingClient(InboxConnectionRouter inboxConnectionRouter) => _inboxConnectionRouter = inboxConnectionRouter;

   public IExactlyOnceInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      _inboxConnectionRouter.ConnectionToHandlerFor(tommand);

   public IReadOnlyList<IExactlyOnceInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent) =>
      _inboxConnectionRouter.SubscriberConnectionsFor(tevent);
}
