using Compze.DocumentDb.Public;
using Compze.Core.Tessaging.Teventive.TeventStore.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tests.Infrastructure;

public static class TestWiringHelper
{
   public static ITeventStore TeventStore(this IServiceResolver @this) =>
      @this.Resolve<ITeventStore>();

   public static ITeventStoreUpdater TeventStoreUpdater(this IServiceResolver @this) =>
      @this.Resolve<ITeventStoreUpdater>();

   public static ITeventStoreReader TeventStoreReader(this IServiceResolver @this) =>
      @this.Resolve<ITeventStoreReader>();

   public static IDocumentDb DocumentDb(this IServiceResolver @this) =>
      @this.Resolve<IDocumentDb>();

   public static IDocumentDbReader DocumentDbReader(this IServiceResolver @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IServiceResolver @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IServiceResolver @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbSession DocumentDbSession(this IServiceResolver @this)
      => @this.Resolve<IDocumentDbSession>();
}
