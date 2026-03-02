using System;
using Compze.Utilities.DependencyInjection.Abstractions;

namespace Compze.Utilities.DependencyInjection.SimpleInjector;

static class LifeStyleConverter
{
   public static global::SimpleInjector.Lifestyle AsSimpleInjectorLifestyle(this Lifestyle @this)
   {
      return @this switch
      {
         Lifestyle.Singleton => global::SimpleInjector.Lifestyle.Singleton,
         Lifestyle.Scoped    => global::SimpleInjector.Lifestyle.Scoped,
         _                   => throw new ArgumentOutOfRangeException(nameof(@this), @this, null)
      };
   }
}
