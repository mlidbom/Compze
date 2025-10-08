using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.AspNetCore.DependencyInjection;

public static class AspNetCoreTransportRegistrar
{
    public static void RegisterAspNetCoreTransport(this IDependencyInjectionContainer container)
    {
        container.Register(
            Singleton.For<IInboxTransport>()
                     .CreatedBy((IServiceLocator serviceLocator, IDependencyInjectionContainer cont)
                                    => new AspNetInboxTransport(serviceLocator, cont)),

            Scoped.For<RpcController>()
                  .CreatedBy((IRemotableMessageSerializer serializer,
                              ITypeMapper typeMapper,
                              Inbox.HandlerExecutionEngine handlerExecutionEngine,
                              Inbox.IMessageStorage messageStorage)
                                 => new RpcController(serializer, typeMapper, handlerExecutionEngine, messageStorage)),
            Scoped.For<TessagingController>()
                  .CreatedBy((IRemotableMessageSerializer serializer,
                              ITypeMapper typeMapper,
                              Inbox.HandlerExecutionEngine handlerExecutionEngine,
                              Inbox.IMessageStorage messageStorage)
                                 => new TessagingController(serializer, typeMapper, handlerExecutionEngine, messageStorage))
        );
    }
}
