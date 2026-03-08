using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

public interface IMicrosoftContainerInternals
{
   IServiceCollection ServiceCollection { get; }
   IServiceProvider ServiceProvider { get; }
   void PushExternalScope(IServiceScope scope);
   void PopExternalScope();
}
