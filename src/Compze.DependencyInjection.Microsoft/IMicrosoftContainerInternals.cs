using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public interface IMicrosoftBuilderInternals
{
   IServiceCollection ServiceCollection { get; }
}

public interface IMicrosoftContainerInternals
{
   IServiceProvider ServiceProvider { get; }
}
