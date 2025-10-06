using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Utilities.DependencyInjection;

namespace Compze.Tessaging.Hosting.Http.DependencyInjection;

public static class HttpTransportRegistrar
{
    public static void RegisterHttpTransport(this IDependencyInjectionContainer container)
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
