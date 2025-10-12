using System;
using System.Threading.Tasks;
using Compze.Sql.DocumentDb.Abstractions;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.DependencyInjection;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using JetBrains.Annotations;
using NUnit.Framework;
using Compze.Tests.Infrastructure.NUnit;

namespace Compze.Tests.Common.NUnit.Sql.DocumentDb;

public abstract class DocumentDbTestsBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected IDocumentDb CreateStore() => ServiceLocator.DocumentDb();
   protected IServiceLocator ServiceLocator { get; private set; }

   static IServiceLocator CreateServiceLocator() => TestEnv.DIContainer.SetupTestingServiceLocator(_ => {});

   [SetUp] public void Setup() => ServiceLocator = CreateServiceLocator();

   [TearDown] public async Task TearDownTask() => await ServiceLocator.DisposeAsync();

   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) => ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));
   public void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
}
