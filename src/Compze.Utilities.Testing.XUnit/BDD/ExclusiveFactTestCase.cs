using System;
using System.Collections.Generic;

namespace Compze.Utilities.Testing.XUnit.BDD;

class ExclusiveFactTestCase : ConstructorArgumentForwardingTestCase
{
   [Obsolete("Called by deserializer")]
   // ReSharper disable once UnusedMember.Global
   public ExclusiveFactTestCase() {}

#pragma warning disable IDE0290
   public ExclusiveFactTestCase(TestCaseDetails details, Dictionary<string, HashSet<string>> traits)
      : base(details, traits: traits) {}
#pragma warning restore IDE0290
}
