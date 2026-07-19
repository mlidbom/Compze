using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.Hosting.Testing.Wiring;

public static class ContainerCloner
{
   ///<summary>Builds a new container from a clone of this container's registrations. Cloned containers share the root container's database pool and serializers; see <see cref="TestingComponentRegistrarDbPool.CurrentTestsDbPoolIfNotCloneContainer(IComponentRegistrar)"/>.</summary>
   public static IDependencyInjectionContainer CloneAndBuild(this IDependencyInjectionContainer @this) =>
      @this.CreateCloneContainerBuilder().Build();
}
