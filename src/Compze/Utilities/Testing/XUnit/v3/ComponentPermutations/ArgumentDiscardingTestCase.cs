using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ArgumentDiscardingTestCase : XunitTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public ArgumentDiscardingTestCase() {}

   public ArgumentDiscardingTestCase(XunitTestCase testCase)
      : base(testMethod: testCase.TestMethod,
             testCaseDisplayName: testCase.TestCaseDisplayName,
             uniqueID: testCase.UniqueID,
             @explicit: testCase.Explicit,
             skipExceptions: testCase.SkipExceptions,
             skipReason: testCase.SkipReason,
             skipType: testCase.SkipType,
             skipUnless: testCase.SkipUnless,
             skipWhen: testCase.SkipWhen,
             traits: testCase.Traits,
             sourceFilePath: testCase.SourceFilePath,
             sourceLineNumber: testCase.SourceLineNumber,
             timeout: testCase.Timeout,
             testMethodArguments: []) // This test case does not pass arguments to the test method
   {}
}
