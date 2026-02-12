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
   public string TestCaseDisplayName => details.TestCaseDisplayName;
   public bool Explicit => details.Explicit;
   public Type[]? SkipExceptions => details.SkipExceptions;
   public string? SkipReason => details.SkipReason;
   public Type? SkipType => details.SkipType;
   public string? SkipUnless => details.SkipUnless;
   public string? SkipWhen => details.SkipWhen;
   public string? SourceFilePath => details.SourceFilePath;
   public int? SourceLineNumber => details.SourceLineNumber;
   public int Timeout => details.Timeout;
   public string UniqueID => details.UniqueID;
   public IXunitTestMethod ResolvedTestMethod => details.ResolvedTestMethod;
}
