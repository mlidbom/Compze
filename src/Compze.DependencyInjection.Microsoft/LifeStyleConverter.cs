using Compze.DependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.DependencyInjection.Microsoft;

static class LifeStyleConverter
{
   public static ServiceLifetime AsServiceLifetime(this Lifestyle @this)
   {
      return @this switch
      {
         Lifestyle.Singleton => ServiceLifetime.Singleton,
         Lifestyle.Scoped    => ServiceLifetime.Scoped,
         _                   => throw new ArgumentOutOfRangeException(nameof(@this))
      };
   }
}
