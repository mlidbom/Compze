using LightInject;

namespace Compze.DependencyInjection.LightInject;

public interface ILightInjectBuilderInternals
{
   IServiceContainer ServiceContainer { get; }
}

public interface ILightInjectContainerInternals
{
   IServiceContainer ServiceContainer { get; }
}
