using Autofac.Builder;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection.Autofac;

static class LifeStyleConverter
{
   public static IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> WithCompzeLifestyle<TLimit, TActivatorData, TRegistrationStyle>(
      this IRegistrationBuilder<TLimit, TActivatorData, TRegistrationStyle> @this, Lifestyle lifestyle)
   {
      return lifestyle switch
      {
         Lifestyle.Singleton  => @this.SingleInstance(),
         Lifestyle.Scoped     => @this.InstancePerLifetimeScope(),
         Lifestyle.TrackedTransient  => @this.InstancePerDependency(),
         _                    => throw new ArgumentOutOfRangeException(nameof(lifestyle), lifestyle, null)
      };
   }
}
