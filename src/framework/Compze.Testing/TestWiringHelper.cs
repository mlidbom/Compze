using System;
using Compze.DependencyInjection;
using Compze.Logging;
using Compze.Messaging.Buses;
using Compze.Persistence.Common.DependencyInjection;
using Compze.Persistence.DocumentDb;
using Compze.Persistence.EventStore;
using Compze.Testing.DependencyInjection;
using JetBrains.Annotations;

namespace Compze.Testing;

static class TestWiringHelper
{
   const string DocumentDbConnectionStringName = "Fake_connectionstring_for_database_testing";
   internal const string EventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

   internal static IEventStore EventStore(this IServiceLocator @this) =>
      @this.Resolve<IEventStore>();

   internal static IDocumentDb DocumentDb(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDb>();

   internal static IDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbReader>();

   internal static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   internal static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   internal static IEventStoreUpdater EventStoreUpdater(this IServiceLocator @this) =>
      @this.Resolve<IEventStoreUpdater>();

   internal static IEventStoreReader EventStoreReader(this IServiceLocator @this) =>
      @this.Resolve<IEventStoreReader>();

   internal static IDocumentDbSession DocumentDbSession(this IServiceLocator @this)
      => @this.Resolve<IDocumentDbSession>();

   static void RegisterTestingDocumentDb(this IDependencyInjectionContainer @this) => @this.RegisterDocumentDb(DocumentDbConnectionStringName);

   static void RegisterTestingEventStore(this IDependencyInjectionContainer @this) => @this.RegisterEventStore(EventStoreConnectionStringName);

   internal static IServiceLocator SetupTestingServiceLocator([InstantHandle] Action<IEndpointBuilder>? configureContainer = null) =>
      CompzeLogger.For(typeof(TestWiringHelper)).ExceptionsAndRethrow(() =>
                                                                   TestingContainerFactory.CreateServiceLocatorForTesting(container =>
                                                                   {
                                                                      container.Container.RegisterTestingDocumentDb();
                                                                      container.Container.RegisterTestingEventStore();
                                                                      configureContainer?.Invoke(container);
                                                                   }));
}