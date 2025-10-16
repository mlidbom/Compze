using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
#pragma warning disable CA1812 // Avoid uninstantiated internal classes : This class is instantiated by xUnit via reflection.
class PerformanceDiscoverer : ITraitDiscoverer
#pragma warning restore CA1812
{
   public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
   {
      yield return new KeyValuePair<string, string>("Category", "Performance");
   }
}
