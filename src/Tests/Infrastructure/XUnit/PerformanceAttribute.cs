using System;
using Compze.Utilities.SystemCE.ReflectionCE;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TraitDiscoverer(PerformanceDiscovererFullTypeName, PerformanceDiscovererAssembly)]
public sealed class PerformanceAttribute : Attribute, ITraitAttribute
{
   const string PerformanceDiscovererFullTypeName = "Compze.Tests.Infrastructure.XUnit.PerformanceDiscoverer";
   const string PerformanceDiscovererAssembly = "Compze.Tests.Infrastructure";

   static PerformanceAttribute()
   {
      Invariant.Is(PerformanceDiscovererFullTypeName == typeof(PerformanceDiscoverer).GetFullNameCompilable(),
                   () =>
                      $"""
                       Expected: {typeof(PerformanceDiscoverer).GetFullNameCompilable()}
                       Found   : {PerformanceDiscovererFullTypeName}
                       """);
      Invariant.Is(PerformanceDiscovererAssembly == typeof(PerformanceDiscoverer).Assembly.GetName().Name,
                   () =>
                      $"""
                       Expected: {typeof(PerformanceDiscoverer).Assembly.GetName().Name}
                       Found   : {PerformanceDiscovererAssembly}
                       """);
   }
}
