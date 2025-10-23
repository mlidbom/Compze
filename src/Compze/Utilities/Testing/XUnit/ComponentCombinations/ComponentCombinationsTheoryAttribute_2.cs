using System;
using System.Linq;
using Compze.Utilities.Functional;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

public abstract class ComponentCombinationsTheoryAttribute<TComponent1, TComponent2> : ComponentCombinationsTheoryAttribute
   where TComponent1 : Enum
   where TComponent2 : Enum
{
   protected ComponentCombinationsTheoryAttribute(string configurationFileName,
                                                  bool useTestMethodArgument,
                                                  string? sourceFilePath,
                                                  int sourceLineNumber)
      : base(configurationFileName: configurationFileName,
             componentEnumTypes: EnumerableCE.OfTypes<TComponent1, TComponent2>().ToArray(),
             useTestMethodArgument: useTestMethodArgument,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber) {}
}
