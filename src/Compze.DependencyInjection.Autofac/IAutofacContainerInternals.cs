using Autofac;

namespace Compze.DependencyInjection.Autofac;

public interface IAutofacContainerInternals
{
   IContainer Container { get; }
   ContainerBuilder ContainerBuilder { get; }
}
