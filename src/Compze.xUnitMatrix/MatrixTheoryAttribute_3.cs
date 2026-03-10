using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

public abstract class MatrixTheoryAttribute<TComponent1, TComponent2, TComponent3>(
   string? configurationFileName,
   bool useTestMethodArgument,
   string? sourceFilePath,
   int sourceLineNumber)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          componentEnumTypes: EnumerableCE.OfTypes<TComponent1, TComponent2, TComponent3>().ToArray(),
                                          useTestMethodArgument: useTestMethodArgument,
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TComponent1 : Enum
   where TComponent2 : Enum
   where TComponent3 : Enum
{
   public static TComponent1 CurrentComponent1 => GetCurrentComponent<TComponent1>(0);
   public static TComponent2 CurrentComponent2 => GetCurrentComponent<TComponent2>(1);
   public static TComponent3 CurrentComponent3 => GetCurrentComponent<TComponent3>(2);
}
