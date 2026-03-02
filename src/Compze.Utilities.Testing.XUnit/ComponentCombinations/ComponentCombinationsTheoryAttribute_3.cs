using System;
using System.Linq;
using Compze.Underscore;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

public abstract class ComponentCombinationsTheoryAttribute<TComponent1, TComponent2, TComponent3>(
   string configurationFileName,
   bool useTestMethodArgument,
   string? sourceFilePath,
   int sourceLineNumber)
   : ComponentCombinationsTheoryAttribute(configurationFileName: configurationFileName,
                                          componentEnumTypes: EnumerableCE.OfTypes<TComponent1, TComponent2, TComponent3>().ToArray(),
                                          useTestMethodArgument: useTestMethodArgument,
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TComponent1 : Enum
   where TComponent2 : Enum
   where TComponent3 : Enum;
