using Autofac;

namespace Compze.DependencyInjection.Autofac;

public interface IAutofacBuilderInternals
{
   ContainerBuilder ContainerBuilder { get; }
}

public interface IAutofacContainerInternals
{
   IContainer Container { get; }
}
