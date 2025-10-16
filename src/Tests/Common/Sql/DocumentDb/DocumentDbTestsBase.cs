using System;
using System.Threading.Tasks;
using Compze.Sql.DocumentDb.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using Xunit;

namespace Compze.Tests.Common.Sql.DocumentDb;

public abstract class DocumentDbTestsBase : UniversalTestBase, IAsyncLifetime
{
   protected IServiceLocator ServiceLocator { get; } = TestEnv.DIContainer.SetupTestingServiceLocator(_ => {});

   public async Task InitializeAsync() => await Task.CompletedTask;
   public async Task DisposeAsync() => await ServiceLocator.DisposeAsync();

   protected IDocumentDb CreateStore() => ServiceLocator.DocumentDb();

   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) =>
      ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));

   protected void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
}
