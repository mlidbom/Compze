using System;
using Compze.Persistence.DocumentDb.Abstractions;
using Compze.Persistence.DocumentDb.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using JetBrains.Annotations;

namespace Compze.Tests.Infrastructure;

static class TestWiringHelper
{
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

   internal static IServiceLocator SetupTestingServiceLocator([InstantHandle] Action<IDependencyRegistrar>? configureContainer = null) =>
      CompzeLogger.For(typeof(TestWiringHelper)).ExceptionsAndRethrow(() =>
                                                                   TestingContainerFactory.CreateServiceLocatorForTesting(register =>
                                                                   {
                                                                      register.DocumentDb();
                                                                      register.EventStore(EventStoreConnectionStringName);
                                                                      configureContainer?.Invoke(register);
                                                                   }));
}