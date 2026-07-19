using Compze.DocumentDb.Public;
using Compze.Hosting.Testing;
using Compze.Internals.Testing;
using Compze.Tests.Common.Wiring;
using Compze.Tests.Infrastructure;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using Xunit;

namespace Compze.Tests.Common.Sql.DocumentDb;

public abstract class DocumentDbTestsBase : UniversalTestBase
{
   protected IDependencyInjectionContainer Container { get; } = TestEnv.DIContainer.SetupTestingContainer(registrar => registrar.RequireCommonTestTypeMappings());

   protected override async Task DisposeAsyncInternal() => await Container.DisposeAsync();


   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) =>
      Container.ExecuteUnitOfWork(unitOfWork => useSession(unitOfWork.DocumentDbReader(), unitOfWork.DocumentDbUpdater()));

   protected void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => Container.ExecuteInIsolatedScope(scope => useSession(scope.DocumentDbReader()));
}
