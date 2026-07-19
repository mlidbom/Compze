using Compze.DependencyInjection.Wiring.Registration;

namespace Compze.DependencyInjection.Runtime.Resolution;

///<summary>
/// <para>>When resolved through <see cref="Resolver"/>> all services registered as <see cref="Lifestyle.Scoped"/> will resolve as the same exact instance, separate from the instance returned by any other <see cref="IScopeResolver"/></para>
/// <para>Dispose will dispose All <see cref="Lifestyle.Scoped"/> or <see cref="Lifestyle.TrackedTransient"/> services resolved through <see cref="Resolver"/></para>
///
/// </summary>
public interface IScope : IDisposable
{
   IScopeResolver Resolver { get; }
}
