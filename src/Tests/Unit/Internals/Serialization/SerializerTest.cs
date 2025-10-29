using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Serialization.Internal;
using Compze.Tessaging.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tests.Infrastructure;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Functional;

namespace Compze.Tests.Unit.Internals.Serialization;

public class SerializerTest : UniversalTestBase
{
   internal ITeventStoreSerializer TeventSerializer => _container.ServiceLocator.Resolve<ITeventStoreSerializer>();
   internal IDocumentDbSerializer DocumentSerializer => _container.ServiceLocator.Resolve<IDocumentDbSerializer>();

   readonly IDependencyInjectionContainer _container;

   protected SerializerTest()
   {
      _container = TestEnv.DIContainer
                         .CreateWithCurrentTestsPluggableComponents()
                         .mutate(it => it.Register().TypeMapper());
   }

   protected override void DisposeInternal() => _container.Dispose();
}
