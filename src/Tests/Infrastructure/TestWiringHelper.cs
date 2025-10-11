using System;
using Compze.Sql.DocumentDb.Abstractions;
using Compze.Sql.DocumentDb.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using JetBrains.Annotations;

namespace Compze.Tests.Infrastructure;

public static class TestWiringHelper
{
   public const string EventStoreConnectionStringName = "Fake_connectionstring_for_database_testing";

   public static IEventStore EventStore(this IServiceLocator @this) =>
      @this.Resolve<IEventStore>();

   public static IDocumentDb DocumentDb(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IEventStoreUpdater EventStoreUpdater(this IServiceLocator @this) =>
      @this.Resolve<IEventStoreUpdater>();

   public static IEventStoreReader EventStoreReader(this IServiceLocator @this) =>
      @this.Resolve<IEventStoreReader>();

   public static IDocumentDbSession DocumentDbSession(this IServiceLocator @this)
      => @this.Resolve<IDocumentDbSession>();

   public static IServiceLocator SetupTestingServiceLocator([InstantHandle] Action<IDependencyRegistrar>? configureContainer = null) =>
      CompzeLogger.For(typeof(TestWiringHelper)).ExceptionsAndRethrow(() =>
                                                                   TestingContainerFactory.CreateServiceLocatorForTesting(register =>
                                                                   {
                                                                      register.DocumentDb();
                                                                      register.EventStore(EventStoreConnectionStringName);
                                                                      configureContainer?.Invoke(register);
                                                                   }));
}