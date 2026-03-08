using Autofac;

namespace Compze.DependencyInjection.Autofac;

public interface IAutofacContainerInternals
{
   ILifetimeScope LifetimeScope { get; }
   ContainerBuilder ContainerBuilder { get; }
}
