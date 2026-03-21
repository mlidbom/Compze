using Autofac;

namespace Compze.DependencyInjection.Autofac;

public interface IAutofacBuilderInternals
{
   global::Autofac.ContainerBuilder ContainerBuilder { get; }
}

public interface IAutofacContainerInternals
{
   IContainer Container { get; }
}
