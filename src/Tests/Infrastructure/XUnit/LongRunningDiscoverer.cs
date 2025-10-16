using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Compze.Tests.Infrastructure.XUnit;

class LongRunningDiscoverer : ITraitDiscoverer
#pragma warning restore CA1812
{
   public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
   {
      yield return new KeyValuePair<string, string>("Category", "LongRunning");
   }
}
