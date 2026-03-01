using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Serialization.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Underscore;

namespace Compze.Tests.Unit.Internals.Serialization;

public class SerializerTest : UniversalTestBase
{
   internal ITeventStoreSerializer TeventSerializer => _container.ServiceLocator.Resolve<ITeventStoreSerializer>();
   internal IDocumentDbSerializer DocumentSerializer => _container.ServiceLocator.Resolve<IDocumentDbSerializer>();

   readonly IDependencyInjectionContainer _container;

   protected SerializerTest()
   {
#pragma warning disable CA2000// We are disposing this disposable just a few lines down....
      _container = TestEnv.DIContainer
                         .CreateWithCurrentTestsPluggableComponents()
                         ._mutate(it => it.Register().TypeMapper());
#pragma warning disable CA2000
   }

   protected override void DisposeInternal() => _container.Dispose();
}
