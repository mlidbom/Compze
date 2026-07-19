using Compze.DependencyInjection.Wiring.Registration;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

static class LifeStyleConverter
{
   public static ServiceLifetime AsServiceLifetime(this Lifestyle @this)
   {
      return @this switch
      {
         Lifestyle.Singleton  => ServiceLifetime.Singleton,
         Lifestyle.Scoped     => ServiceLifetime.Scoped,
         Lifestyle.TrackedTransient  => ServiceLifetime.Transient,
         _                    => throw new ArgumentOutOfRangeException(nameof(@this))
      };
   }
}
