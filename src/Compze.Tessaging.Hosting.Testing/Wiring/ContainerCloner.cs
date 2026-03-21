using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class ContainerCloner
{
   public static IDependencyInjectionContainer CloneAndBuild(this IDependencyInjectionContainer @this) =>
      @this.CreateCloneContainerBuilder().Build();
}
