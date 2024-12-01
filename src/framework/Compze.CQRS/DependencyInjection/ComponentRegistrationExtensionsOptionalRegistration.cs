using System.Linq;

namespace Compze.DependencyInjection;

static class ComponentRegistrationExtensionsOptionalRegistration
{
   public static bool HasComponent<TComponent>(this IDependencyInjectionContainer @this) =>
      @this.RegisteredComponents().Any(component => component.ServiceTypes.Contains(typeof(TComponent)));
}