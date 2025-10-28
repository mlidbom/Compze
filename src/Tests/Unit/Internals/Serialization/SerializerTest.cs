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
   internal readonly ITeventStoreSerializer TeventSerializer;
   protected readonly IDependencyInjectionContainer Container;

   public SerializerTest()
   {
      Container = TestEnv.DIContainer
                         .CreateWithCurrentTestsPluggableComponents()
                         .mutate(it => it.Register().TypeMapper());
      TeventSerializer = Container.ServiceLocator.Resolve<ITeventStoreSerializer>();
   }

   protected override void DisposeInternal() => Container.Dispose();
}
