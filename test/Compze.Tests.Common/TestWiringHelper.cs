using Compze.DocumentDb;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Teventive.TeventStore.Abstractions;

namespace Compze.Tests.Common;

public static class TestWiringHelper
{
   public static ITeventStore TeventStore(this IScopeResolver @this) =>
      @this.Resolve<ITeventStore>();


   public static IDocumentDb DocumentDb(this IScopeResolver @this) =>
      @this.Resolve<IDocumentDb>();

   public static ITeventStoreUpdater TeventStoreUpdater(this IScopeResolver @this) =>
      @this.Resolve<ITeventStoreUpdater>();

   public static ITeventStoreReader TeventStoreReader(this IScopeResolver @this) =>
      @this.Resolve<ITeventStoreReader>();

   public static IDocumentDbReader DocumentDbReader(this IScopeResolver @this) =>
      @this.Resolve<IDocumentDbReader>();

   public static IDocumentDbUpdater DocumentDbUpdater(this IScopeResolver @this) =>
      @this.Resolve<IDocumentDbUpdater>();

   public static IDocumentDbBulkReader DocumentDbBulkReader(this IScopeResolver @this) =>
      @this.Resolve<IDocumentDbBulkReader>();

   public static IDocumentDbSession DocumentDbSession(this IScopeResolver @this)
      => @this.Resolve<IDocumentDbSession>();
}
