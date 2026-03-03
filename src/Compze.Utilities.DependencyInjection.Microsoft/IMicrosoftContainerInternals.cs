using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Microsoft;

public interface IMicrosoftContainerInternals
{
   IServiceCollection ServiceCollection { get; }
   IServiceProvider ServiceProvider { get; }
}
