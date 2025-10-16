using System;
using System.Threading.Tasks;
using Compze.Sql.DocumentDb.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;

namespace Compze.Tests.Common.Sql.DocumentDb;

public abstract class DocumentDbTestsBase : UniversalTestBase
{
   protected IDocumentDb CreateStore() => ServiceLocator.DocumentDb();
   protected IServiceLocator ServiceLocator { get; set; }
   static IServiceLocator CreateServiceLocator() => TestEnv.DIContainer.SetupTestingServiceLocator(_ => {});
   
   public virtual void Setup() => ServiceLocator = CreateServiceLocator();
   public virtual async Task TearDownTask() => await ServiceLocator.DisposeAsync();

   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) =>
      ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));

   public void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
}
