using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DependencyInjection.Runtime.Resolution;

///<summary>
/// An <see cref="IServiceResolver"/> capable only of resolving services registered as <see cref="Lifestyle.Singleton"/> and <see cref="Lifestyle.TrackedTransient"/>.
/// <see cref="Lifestyle.Scoped"/> services cannot be resolved from an instance.
/// </summary>
public interface IRootResolver : IServiceResolver;
