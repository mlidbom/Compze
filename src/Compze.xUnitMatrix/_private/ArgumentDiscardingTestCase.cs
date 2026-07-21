using Xunit.v3;
using Compze.xUnit._internal;

namespace Compze.xUnitMatrix._private;

class ArgumentDiscardingTestCase : ConstructorArgumentForwardingTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public ArgumentDiscardingTestCase() {}

#pragma warning disable IDE0290
   public ArgumentDiscardingTestCase(XunitTestCase testCase)
#pragma warning restore IDE0290
      : base(testCase, testMethodArguments: []) // This test case does not pass arguments to the test method
   {}
}
