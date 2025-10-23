using System;
using Xunit.v3;

namespace Compze.Utilities.Testing.XUnit;

// ReSharper disable PrimaryConstructorParameterCaptureDisallowed
public class TestCaseDetails(
   (string TestCaseDisplayName,
      bool Explicit,
      Type[]? SkipExceptions,
      string? SkipReason,
      Type? SkipType,
      string? SkipUnless,
      string? SkipWhen,
      string? SourceFilePath,
      int? SourceLineNumber,
      int Timeout,
      string UniqueID,
      IXunitTestMethod ResolvedTestMethod) details)
{
   internal string TestCaseDisplayName => details.TestCaseDisplayName;
   internal bool Explicit => details.Explicit;
   internal Type[]? SkipExceptions => details.SkipExceptions;
   internal string? SkipReason => details.SkipReason;
   internal Type? SkipType => details.SkipType;
   internal string? SkipUnless => details.SkipUnless;
   internal string? SkipWhen => details.SkipWhen;
   internal string? SourceFilePath => details.SourceFilePath;
   internal int? SourceLineNumber => details.SourceLineNumber;
   internal int Timeout => details.Timeout;
   internal string UniqueID => details.UniqueID;
   internal IXunitTestMethod ResolvedTestMethod => details.ResolvedTestMethod;
}
