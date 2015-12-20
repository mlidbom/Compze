﻿using AccountManagement.Domain.Events.EventStore.Services;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.Domain.Events.EventStore.ContainerInstallers
{
    public class AccountManagementEventStoreSession : EventStoreSession, IAccountManagementEventStoreSession
    {
        public AccountManagementEventStoreSession(IServiceBus bus, IEventStore store, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource)
            : base(bus, store, usageGuard, timeSource) {}
    }
}
