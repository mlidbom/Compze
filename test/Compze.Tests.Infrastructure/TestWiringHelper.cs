using Compze.DocumentDb.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tests.Infrastructure;

public static class TestWiringHelper
{
   public static ITeventStore TeventStore(this IServiceLocator @this) =>
      @this.Resolve<ITeventStore>();

   public static IDocumentDb DocumentDb(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDbReader DocumentDbReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceLocator @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbSession DocumentDbSession(this IServiceLocator @this)
      => @this.Resolve<IDocumentDbSession>();
}
