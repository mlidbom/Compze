using DryIoc;

namespace Compze.DependencyInjection.DryIoc;

public interface IDryIocBuilderInternals
{
   IContainer Container { get; }
}

public interface IDryIocContainerInternals
{
   IContainer Container { get; }
}
