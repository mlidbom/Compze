using Compze.DocumentDb.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tests.Infrastructure;

public static class TestWiringHelper
{
   public static ITeventStore TeventStore(this IServiceLocator @this) =>
      @this.Resolve<ITeventStore>();

   public static ITeventStore TeventStore(this IServiceScope @this) =>
      @this.Resolve<ITeventStore>();

   public static ITeventStoreUpdater TeventStoreUpdater(this IServiceLocator @this) =>
      @this.Resolve<ITeventStoreUpdater>();

   public static ITeventStoreUpdater TeventStoreUpdater(this IServiceScope @this) =>
      @this.Resolve<ITeventStoreUpdater>();

   public static ITeventStoreReader TeventStoreReader(this IServiceLocator @this) =>
      @this.Resolve<ITeventStoreReader>();

   public static ITeventStoreReader TeventStoreReader(this IServiceScope @this) =>
      @this.Resolve<ITeventStoreReader>();

   public static IDocumentDb DocumentDb(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDb DocumentDb(this IServiceScope @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbReader DocumentDbReader(this IServiceScope @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceScope @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceScope @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbSession DocumentDbSession(this IServiceLocator @this)
      => @this.Resolve<IDocumentDbSession>();

   public static IDocumentDbSession DocumentDbSession(this IServiceScope @this)
      => @this.Resolve<IDocumentDbSession>();
}
