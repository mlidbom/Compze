using Compze.Utilities.DependencyInjection.Abstractions;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Compze.Utilities.DependencyInjection.Microsoft;

public static class LifeStyleConverter
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
