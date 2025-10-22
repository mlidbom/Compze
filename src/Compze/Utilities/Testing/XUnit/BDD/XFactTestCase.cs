using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.BDD;

public class XFactTestCase : ConstructorArgumentForwardingTestCase
{
   [Obsolete("Called by deserializer")]
   // ReSharper disable once UnusedMember.Global
   public XFactTestCase() {}

   public XFactTestCase(
      TestCaseDetails details,
      Dictionary<string, HashSet<string>> traits)
      : base(details,
             traits: traits)
   {
   }
}
