using Compze.xUnit;

namespace Compze.xUnitBDD;

class ExclusiveFactTestCase : ConstructorArgumentForwardingTestCase
{
   [Obsolete("Called by deserializer")]
   // ReSharper disable once UnusedMember.Global
   public ExclusiveFactTestCase() {}

#pragma warning disable IDE0290
   // ReSharper disable once ConvertToPrimaryConstructor
   public ExclusiveFactTestCase(TestCaseDetails details, Dictionary<string, HashSet<string>> traits)
      : base(details, traits: traits) {}
#pragma warning restore IDE0290
}
