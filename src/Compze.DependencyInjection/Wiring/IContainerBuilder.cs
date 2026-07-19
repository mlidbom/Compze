using Compze.DependencyInjection.Runtime;
using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DependencyInjection.Wiring;

/// <summary>
/// Composes <see cref="IComponentRegistrar"/> (for registration) and <see cref="Build"/> (for finalization).
/// Registration is performed through <see cref="Registrar"/>. The builder itself has no registration methods.
/// After <see cref="Build"/> is called, the builder should not be used further — it is a spent object.
/// Not disposable: the built <see cref="IDependencyInjectionContainer"/> owns all resources.
/// </summary>
public interface IContainerBuilder
{
   IComponentRegistrar Registrar { get; }
   IDependencyInjectionContainer Build(ContainerOptions? options = null);
}
