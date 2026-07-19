using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DependencyInjection.Runtime.Resolution;

///<summary>
/// <para>Instances are owned by an <see cref="IScope"/> and resolving components will only work within that scope.</para>
/// <para>All resolved components registered as <see cref="Lifestyle.Scoped"/> will resolve to the same instance and that instance will be disposed when the <see cref="IScope"/> is disposed</para>
/// </summary>
public interface IScopeResolver : IServiceResolver;
