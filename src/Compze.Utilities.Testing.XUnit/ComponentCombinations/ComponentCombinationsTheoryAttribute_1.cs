using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

public abstract class ComponentCombinationsTheoryAttribute<TComponent1>(
   string configurationFileName,
   bool useTestMethodArgument,
   string? sourceFilePath = null,
   int sourceLineNumber = -1)
   : ComponentCombinationsTheoryAttribute(configurationFileName: configurationFileName,
                                          componentEnumTypes: EnumerableCE.OfTypes<TComponent1>().ToArray(),
                                          useTestMethodArgument: useTestMethodArgument,
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TComponent1 : Enum;
