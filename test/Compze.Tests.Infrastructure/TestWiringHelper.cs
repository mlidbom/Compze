using Compze.DocumentDb.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tests.Infrastructure;

public static class TestWiringHelper
{
   public static ITeventStore TeventStore(this IServiceLocator @this) =>
      @this.Resolve<ITeventStore>();

   public static ITeventStore TeventStore(this IServiceLocatorScope @this) =>
      @this.Resolve<ITeventStore>();

   public static ITeventStoreUpdater TeventStoreUpdater(this IServiceLocator @this) =>
      @this.Resolve<ITeventStoreUpdater>();

   public static ITeventStoreUpdater TeventStoreUpdater(this IServiceLocatorScope @this) =>
      @this.Resolve<ITeventStoreUpdater>();

   public static ITeventStoreReader TeventStoreReader(this IServiceLocator @this) =>
      @this.Resolve<ITeventStoreReader>();

   public static ITeventStoreReader TeventStoreReader(this IServiceLocatorScope @this) =>
      @this.Resolve<ITeventStoreReader>();

   public static IDocumentDb DocumentDb(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDb DocumentDb(this IServiceLocatorScope @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbReader DocumentDbReader(this IServiceLocatorScope @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocatorScope @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocatorScope @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbSession DocumentDbSession(this IServiceLocator @this)
      => @this.Resolve<IDocumentDbSession>();

   public static IDocumentDbSession DocumentDbSession(this IServiceLocatorScope @this)
      => @this.Resolve<IDocumentDbSession>();
}
