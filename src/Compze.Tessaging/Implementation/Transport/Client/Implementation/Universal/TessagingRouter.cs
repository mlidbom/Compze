using System.Collections.Generic;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;

public class TessagingRouter : ITessagingRouter
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessagingRouter>().CreatedBy(
            (InboxConnectionRouter router) => new TessagingRouter(router)));

   readonly InboxConnectionRouter _inboxConnectionRouter;

   TessagingRouter(InboxConnectionRouter inboxConnectionRouter) => _inboxConnectionRouter = inboxConnectionRouter;

   public ITessagingInboxConnection ConnectionToHandlerFor(IRemotableTommand tommand) =>
      _inboxConnectionRouter.ConnectionToHandlerFor(tommand);

   public IReadOnlyList<ITessagingInboxConnection> SubscriberConnectionsFor(IExactlyOnceTevent tevent) =>
      _inboxConnectionRouter.SubscriberConnectionsFor(tevent);
}
