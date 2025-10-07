using System;
using System.Threading.Tasks;
using Compze.Persistence.DocumentDb.Abstractions;
using Compze.Testing;
using Compze.Utilities.DependencyInjection;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Compze.Tests.Persistence.DocumentDb;

abstract class DocumentDbTestsBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected IDocumentDb CreateStore() => ServiceLocator.DocumentDb();
   protected IServiceLocator ServiceLocator { get; private set; }

   static IServiceLocator CreateServiceLocator() =>
      TestWiringHelper.SetupTestingServiceLocator(builder => {});

   [SetUp] public void Setup() => ServiceLocator = CreateServiceLocator();

   [TearDown] public async Task TearDownTask() => await ServiceLocator.DisposeAsync();

   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) => ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));
   internal void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
}
