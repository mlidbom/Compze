using System;
using Compze.Sql.DocumentDb.Abstractions;
using Compze.Sql.DocumentDb.DependencyInjection;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Teventive.EventStore.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Logging;
using JetBrains.Annotations;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure;

public static class TestWiringHelper
{
   static (Wiring.SqlLayer, Wiring.DIContainer) ParseParts(string _combination)
   {
      try
      {
         var parts = _combination.Split(':');

         Argument.Is(parts.Length == 2, () => $"PluggableComponentParts has an invalid format: {_combination}");

         return ((Wiring.SqlLayer)Enum.Parse(typeof(Wiring.SqlLayer), parts[0]),
                 (Wiring.DIContainer)Enum.Parse(typeof(Wiring.DIContainer), parts[1]));
      }
      catch(Exception e)
      {
         throw new Exception($"PluggableComponentParts has an invalid format: {_combination}", e);
      }
   }

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
}