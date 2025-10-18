using System;
using Compze.Utilities.SystemCE.ReflectionCE;
using Xunit.Sdk;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tests.Infrastructure.XUnit;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[TraitDiscoverer(LongRunningDiscovererFullTypeName, LongRunningDiscovererAssembly)]
public sealed class LongRunningAttribute : Attribute, ITraitAttribute
{
   const string LongRunningDiscovererFullTypeName = "Compze.Tests.Infrastructure.XUnit.LongRunningDiscoverer";
   const string LongRunningDiscovererAssembly = "Compze.Tests.Infrastructure";

   static LongRunningAttribute()
   {
      Invariant.Is(LongRunningDiscovererFullTypeName == typeof(LongRunningDiscoverer).GetFullNameCompilable(),
                   () =>
                      $"""
                       Expected: {typeof(LongRunningDiscoverer).GetFullNameCompilable()}
                       Found   : {LongRunningDiscovererFullTypeName}
                       """);
      Invariant.Is(LongRunningDiscovererAssembly == typeof(LongRunningDiscoverer).Assembly.GetName().Name,
                   () =>
                      $"""
                       Expected: {typeof(LongRunningDiscoverer).Assembly.GetName().Name}
                       Found   : {LongRunningDiscovererAssembly}
                       """);
   }
}
