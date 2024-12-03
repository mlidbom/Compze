using System;
using System.Threading.Tasks;
using Compze.DependencyInjection;
using Compze.Persistence.DocumentDb;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Compze.Testing;
using JetBrains.Annotations;
using NUnit.Framework;

namespace Compze.Tests.Persistence.DocumentDb;

abstract class DocumentDbTestsBase(string pluggableComponentsCombination) : DuplicateByPluggableComponentTest(pluggableComponentsCombination)
{
   protected IDocumentDb CreateStore() => ServiceLocator.DocumentDb();
   protected IServiceLocator ServiceLocator { get; private set; }

   static IServiceLocator CreateServiceLocator() =>
      TestWiringHelper.SetupTestingServiceLocator(builder => builder.TypeMapper
                                                                    .Map<User>("96f37428-04ca-4f60-858a-785d26ee7576")
                                                                    .Map<Email>("648191d9-bfae-45c0-b824-d322d01fa64c")
                                                                    .Map<Dog>("ca527ca3-d352-4674-9133-2747756f45b3")
                                                                    .Map<Person>("64133a9b-1279-4029-9469-2d63d4f9ceaa")
                                                                    .Map<System.Collections.Generic.HashSet<User>>("df57e323-d4b0-44c1-a69c-5ea100af9ebf"));

   [SetUp] public void Setup() => ServiceLocator = CreateServiceLocator();

   [TearDown] public async Task TearDownTask()
   {
      if(ServiceLocator != null) await ServiceLocator.DisposeAsync().CaF();
   }

   protected void UseInTransactionalScope([InstantHandle] Action<IDocumentDbReader, IDocumentDbUpdater> useSession) => ServiceLocator.ExecuteTransactionInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader(), ServiceLocator.DocumentDbUpdater()));
   internal void UseInScope([InstantHandle] Action<IDocumentDbReader> useSession) => ServiceLocator.ExecuteInIsolatedScope(() => useSession(ServiceLocator.DocumentDbReader()));
}
