using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// The purpose of this class is just to act as a go-between so that
/// classes that get their data from existing test cases do not need to perform all this ceremony
/// of passing arguments along, losing the actual logic in the noise
/// </summary>
public class ConstructorArgumentForwardingTestCase : XunitTestCase
{
   // ReSharper disable once UnusedMember.Global
   [Obsolete("Called by deserializer", error: true)]
   public ConstructorArgumentForwardingTestCase() {}

   ///<summary>
   /// Passes along every value in the <paramref name="testCase"/>
   /// unless a non-null value is provided for one of the optional arguments,
   /// if it is, that value is used for that argument
   /// </summary>
   public ConstructorArgumentForwardingTestCase(XunitTestCase testCase,
                                                IXunitTestMethod? testMethod = null,
                                                string? testCaseDisplayName = null,
                                                string? uniqueID = null,
                                                bool? @explicit = null,
                                                Type[]? skipExceptions = null,
                                                string? skipReason = null,
                                                Type? skipType = null,
                                                string? skipUnless = null,
                                                string? skipWhen = null,
                                                Dictionary<string, HashSet<string>>? traits = null,
                                                object?[]? testMethodArguments = null,
                                                string? sourceFilePath = null,
                                                int? sourceLineNumber = null,
                                                int? timeout = null)
      : base(testMethod: testCase.TestMethod,
             testCaseDisplayName: testCaseDisplayName ?? testCase.TestCaseDisplayName,
             uniqueID: uniqueID ?? testCase.UniqueID,
             @explicit: @explicit ?? testCase.Explicit,
             skipExceptions: skipExceptions ?? testCase.SkipExceptions,
             skipReason: skipReason ?? testCase.SkipReason,
             skipType: skipType ?? testCase.SkipType,
             skipUnless: skipUnless ?? testCase.SkipUnless,
             skipWhen: skipWhen ?? testCase.SkipWhen,
             traits: traits ?? testCase.Traits,
             sourceFilePath: sourceFilePath ?? testCase.SourceFilePath,
             sourceLineNumber: sourceLineNumber ?? testCase.SourceLineNumber,
             timeout: timeout ?? testCase.Timeout,
             testMethodArguments: testMethodArguments ?? testCase.TestMethodArguments)
   {}
}
