using System;
using System.Collections.Generic;

namespace Compze.Utilities.Testing.XUnit.BDD;

class ExclusiveFactTestCase : ConstructorArgumentForwardingTestCase
{
   [Obsolete("Called by deserializer")]
   // ReSharper disable once UnusedMember.Global
   public ExclusiveFactTestCase() {}

   public ExclusiveFactTestCase(
      TestCaseDetails details,
      Dictionary<string, HashSet<string>> traits)
      : base(details,
             traits: traits)
   {
   }
}
