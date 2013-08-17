﻿#region usings

using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Transactions;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.CQRS.ServiceBus.NServiceBus;
using Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration;
using Composable.CQRS.Testing;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.ServiceBus;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using CQRS.Tests;
using JetBrains.Annotations;
using NCrunch.Framework;
using NServiceBus;
using NUnit.Framework;
using Composable.CQRS.Windsor;

#endregion

namespace Composable.CQRS.ServiceBus.NServicebus.Tests.TransactionSupport
{
    [TestFixture, NUnit.Framework.Category("NSBFullSetupTests")]
    [ExclusivelyUses(NCrunchExlusivelyUsesResources.DocumentDbMdf, NCrunchExlusivelyUsesResources.EventStoreDbMdf, NCrunchExlusivelyUsesResources.NServiceBus)]
    [NCrunch.Framework.Isolated]
    public class WhenMessageHandlingFails
    {
        public static readonly string DocumentDbConnectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        public static readonly string EventStoreConnectionString = ConfigurationManager.ConnectionStrings["EventStore"].ConnectionString;

        [Test]
        [NCrunch.Framework.Isolated]
        public void StoredEventsAreRemoved()
        {
            var endpointConfigurer = new EndPointConfigurer("Composable.CQRS.ServiceBus.NServicebus.Tests.TransactionSupport");

            var eventStoreReader = new SqlServerEventStore(EventStoreConnectionString);

            eventStoreReader.ResetDB();
            SqlServerObjectStore.ResetDB(EventStoreConnectionString);
            SqlServerObjectStore.ResetDB(DocumentDbConnectionString);

            eventStoreReader.SaveEvents(((IEventStored) new Aggregate(2)).GetChanges());

            endpointConfigurer.Init();
            var bus = endpointConfigurer.Container.Resolve<IServiceBus>();

            var messageHandled = new ManualResetEvent(false);
            TestingSupportMessageModule.OnHandleBeginMessage += transaction =>
                                                                    {
                                                                        transaction.TransactionCompleted += (_, __) => messageHandled.Set();
                                                                    };

            bus.SendLocal(new InsertEventsMessage());

            Assert.That(messageHandled.WaitOne(30.Seconds()), Is.True, "Timed out waiting for message");

            using (var tx = new TransactionScope())
            {
                var events = eventStoreReader.StreamEventsAfterEventWithId(null).ToList();
                Assert.That(events, Has.Count.EqualTo(2));
            }
        }
    }

    public class InsertEventsMessage : IMessage
    {
    }

    public class Aggregate : AggregateRoot<Aggregate>
    {
        //always the same in order to cause an exception while saving multiple instances. 
        private readonly Guid _aggregateId = Guid.Parse("EFEF768C-F37B-426F-A53B-BF28A254C55E");

        public Aggregate(int events)
        {
            RegisterEventHandler<SomeEvent>(e => SetIdBeVerySureYouKnowWhatYouAreDoing(_aggregateId));
            1.Through(events).ForEach(i => ApplyEvent(new SomeEvent()));
        }
    }

    public class SomeEvent : AggregateRootEvent
    {
    }

    [UsedImplicitly]
    public class InseartEventsMessageHandler : IHandleMessages<InsertEventsMessage>
    {
        private readonly IEventStoreSession _session;

        public InseartEventsMessageHandler(IEventStoreSession session)
        {
            _session = session;
        }

        public void Handle(InsertEventsMessage message)
        {
            _session.Save(new Aggregate(5));
        }
    }

    public class EndPointConfigurer : NServicebusEndpointConfigurationBase<EndPointConfigurer>, IConfigureThisEndpoint
    {
        public IWindsorContainer Container;
        private readonly string _queueName;

        public EndPointConfigurer(string queueName)
        {
            _queueName = queueName;
        }

        protected override void ConfigureContainer(IWindsorContainer container)
        {
            Container = container;
            ConfigureContainerTest(container);
        }

        override protected Configure ConfigureLogging(Configure config)
        {
            return config;
        }

        public static void ConfigureContainerTest(IWindsorContainer container)
        {
            container.Register(
                Component.For<IMessageInterceptor>().Instance(EmptyMessageInterceptor.Instance),
                Component.For<IServiceBus>().ImplementedBy<NServiceBusServiceBus>(),
               
                Component.For<IEventStoreSession, IUnitOfWorkParticipant>()
                    .ImplementedBy<EventStoreSession>()
                    .LifeStyle.PerNserviceBusMessage(),

                Component.For<IEventStore, SqlServerEventStore>().ImplementedBy<SqlServerEventStore>()
                    .DependsOn(Dependency.OnValue(typeof(string), WhenMessageHandlingFails.DocumentDbConnectionString))
                    .LifestyleSingleton(),

                Component.For<IObservableObjectStore>().ImplementedBy<SqlServerObjectStore>()
                    .DependsOn(Dependency.OnValue(typeof(string), WhenMessageHandlingFails.EventStoreConnectionString))
                    .LifestyleSingleton(),

                Component.For<IDocumentDbSession, IUnitOfWorkParticipant>().ImplementedBy<DocumentDbSession>()
                    .DependsOn(Dependency.OnValue(typeof(IDocumentDbSessionInterceptor), NullOpDocumentDbSessionInterceptor.Instance))
                    .LifeStyle.PerNserviceBusMessage(),
                Component.For<ISingleContextUseGuard>()
                );
        }

        protected override string InputQueueName { get { return _queueName; } }

        protected override Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.MsmqSubscriptionStorage();
        }
    }
}