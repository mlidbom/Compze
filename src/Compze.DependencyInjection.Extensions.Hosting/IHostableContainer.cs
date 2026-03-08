using Microsoft.Extensions.Hosting;

namespace Compze.DependencyInjection.Extensions.Hosting;

public interface IHostableContainer
{
   void UseAsServiceProviderFor(IHostBuilder hostBuilder);
}
