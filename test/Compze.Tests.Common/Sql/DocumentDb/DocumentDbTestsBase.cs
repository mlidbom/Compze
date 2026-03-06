using Compze.DocumentDb.Public;
using Compze.Tessaging.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using Xunit;

namespace Compze.Tests.Common.Sql.DocumentDb;

public abstract class DocumentDbTestsBase : UniversalTestBase, IAsyncLifetime
{
   protected IServiceLocator ServiceLocator { get; } = TestEnv.DIContainer.SetupTestingServiceLocator(_ => {});

   protected override async Task DisposeAsyncInternal() => await ServiceLocator.DisposeAsync();

   protected IDocumentDb CreateStore() => ServiceLocator.DocumentDb();

   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) =>
      ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));

   protected void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
}
