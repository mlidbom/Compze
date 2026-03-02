using System;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit;

class ArgumentDiscardingTestCase : ConstructorArgumentForwardingTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public ArgumentDiscardingTestCase() {}

   public ArgumentDiscardingTestCase(XunitTestCase testCase)
      : base(testCase, testMethodArguments: []) // This test case does not pass arguments to the test method
   {}
}
