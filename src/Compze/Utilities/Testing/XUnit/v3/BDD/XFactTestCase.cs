using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit.BDD;

public class XFactTestCase : XunitTestCase
{
   [Obsolete("Called by deserializer")]
   public XFactTestCase() {}

   public XFactTestCase(
      IXunitTestMethod testMethod,
      string testCaseDisplayName,
      string uniqueID,
      bool @explicit,
      Type[]? skipExceptions,
      string? skipReason,
      Type? skipType,
      string? skipUnless,
      string? skipWhen,
      Dictionary<string, HashSet<string>> traits,
      string? sourceFilePath,
      int? sourceLineNumber,
      int? timeout)
      : base(testMethod: testMethod,
             testCaseDisplayName: testCaseDisplayName,
             uniqueID: uniqueID,
             @explicit: @explicit,
             skipExceptions: skipExceptions,
             skipReason: skipReason,
             skipType: skipType,
             skipUnless: skipUnless,
             skipWhen: skipWhen,
             traits: traits,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber,
             timeout: timeout)
   {
   }
}
