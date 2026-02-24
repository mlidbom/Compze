using System;
using System.Linq;
using Compze.Functional;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

public abstract class ComponentCombinationsTheoryAttribute<TComponent1> : ComponentCombinationsTheoryAttribute
   where TComponent1 : Enum
{
   protected ComponentCombinationsTheoryAttribute(string configurationFileName,
                                                  bool useTestMethodArgument,
                                                  string? sourceFilePath = null,
                                                  int sourceLineNumber = -1)
      : base(configurationFileName: configurationFileName,
             componentEnumTypes: EnumerableCE.OfTypes<TComponent1>().ToArray(),
             useTestMethodArgument: useTestMethodArgument,
             sourceFilePath: sourceFilePath,
             sourceLineNumber: sourceLineNumber) {}
}
