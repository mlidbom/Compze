using System;
using System.Linq;
using Compze.Utilities.Functional;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentsCombinations;

public abstract class ComponentsCombinationsTheoryAttribute<TComponent1> : ComponentsCombinationsTheoryAttribute
   where TComponent1 : Enum
{
   protected ComponentsCombinationsTheoryAttribute(string configurationFileName,
                                                   object[]? skipped,
                                                   string[]? skipReasons,
                                                   bool useTestMethodArgument,
                                                   string? sourceFilePath,
                                                   int sourceLineNumber)
      : base(configurationFileName: configurationFileName,
             componentEnumTypes: EnumerableCE.OfTypes<TComponent1>().ToArray(),
             skipped: skipped,
             skipReasons: skipReasons,
             useTestMethodArgument: useTestMethodArgument,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber) {}
}
