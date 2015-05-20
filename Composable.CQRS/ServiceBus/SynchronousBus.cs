﻿using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using Composable.KeyValueStorage.Population;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Reflection;
using JetBrains.Annotations;
using NServiceBus;

namespace Composable.ServiceBus
{
    /// <summary>
    /// Sends/Publishes messages to <see cref="IHandleMessages{T}"/> implementations registered in the <see cref="IWindsorContainer"/>.
    /// </summary>
    [UsedImplicitly]
    public class SynchronousBus : IServiceBus
    {
        protected readonly IWindsorContainer Container;
        private readonly List<MessageHandlerResolver> _resolvers;

        public SynchronousBus(IWindsorContainer container,IMessageHandlerResolversProvider resolversProvider=null)
        {
            Container = container;
            resolversProvider = resolversProvider ?? new DefaultMessageHandlerResolversProvider(container);
            _resolvers = resolversProvider.GetResolvers();
        }

        public virtual void Publish(object message)
        {
            PublishLocal(message);
        }

        public virtual bool Handles(object message)
        {
            return _resolvers.Any(invoker => invoker.HasHandlerFor(message));
        }

        protected virtual void PublishLocal(object message)
        {
            using(Container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    foreach(var resolver in _resolvers.Where(resolver => resolver.HasHandlerFor(message)))
                    {
                        var handlers = resolver.ResolveMessageHandlers(message);

                        try
                        {
                            MessageHandlerInvoker.Invoke(handlers, resolver.HandlerInterfaceType, message);
                        }
                        finally
                        {
                            handlers.ForEach(Container.Release);
                        }
                    }

                    transactionalScope.Commit();
                }
            }
        }

        protected virtual void SyncSendLocal(object message)
        {
            using(Container.RequireScope()) //Use the existing scope when running in an endpoint and create a new one if running in the web
            {
                using(var transactionalScope = Container.BeginTransactionalUnitOfWorkScope())
                {
                    if(_resolvers.None(r => r.HasHandlerFor(message)))
                    {
                        throw new NoHandlerException(message.GetType());
                    }

                    AssertOnlyOneHandlerRegistered(message);

                    var resolver = _resolvers.Single(r => r.HasHandlerFor(message));
                    var handlers = resolver.ResolveMessageHandlers(message);

                    try
                    {
                        MessageHandlerInvoker.Invoke(handlers, resolver.HandlerInterfaceType, message);
                    }
                    finally
                    {
                        handlers.ForEach(Container.Release);
                    }

                    transactionalScope.Commit();
                }
            }
        }


        public virtual void SendLocal(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Send(object message)
        {
            SyncSendLocal(message);
        }

        public virtual void Reply(object message)
        {
            SyncSendLocal(message);
        }

        private  void AssertOnlyOneHandlerRegistered(object message)
        {
            var realHandlers = _resolvers
                .Where(r => r.HasHandlerFor(message))
                .SelectMany(r => r.ResolveMessageHandlers(message))
                .ToList();

            if (realHandlers.Except(realHandlers.OfType<ISynchronousBusMessageSpy>()).Count() > 1 || _resolvers.Count(re => re.HasHandlerFor(message)) > 1)
            {
                throw new MultipleMessageHandlersRegisteredException(message, realHandlers);
            }
        }
    }

    public class NoHandlerException : Exception
    {
        public NoHandlerException(Type messageType):base("No handler registered for message type: {0}".FormatWith(messageType.FullName)) { }
    }

    public class MultipleMessageHandlersRegisteredException : Exception
    {
        public MultipleMessageHandlersRegisteredException(object message, List<object> handlers)
            : base(CreateMessage(message, handlers)) { }

        private static string CreateMessage(object message, List<object> handlers)
        {
            var exceptionMessage = "There are multiple handlers registered for the message type:{0}.\nIf you are getting this because you have registered a listener as a test spy have your listener implement ISynchronousBusMessageSpy and the exception will disappear"
                .FormatWith(message.GetType());
            handlers.Select(handler => handler.GetType())
                .ForEach(handlerType => exceptionMessage += "{0}{1}".FormatWith(Environment.NewLine, handlerType.FullName)                
                );
            return exceptionMessage;

        }
    }
}
