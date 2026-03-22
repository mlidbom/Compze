using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Extensions.Hosting;

/// <summary>
/// Integrates a child container with an ASP.NET Core host.
/// The implementation creates a child container from the parent and hooks it into the host's service provider pipeline.
/// Resolved from the parent container — the adapter internally holds the typed parent container reference.
/// </summary>
public interface IChildContainerHostIntegration
{
   void UseChildContainerAsServiceProviderFor(IHostBuilder hostBuilder);
}
