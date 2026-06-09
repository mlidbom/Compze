using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

public abstract class MatrixTheoryAttribute<TDimension1, TDimension2, TDimension3>(
   string? configurationFileName,
   bool useTestMethodArgument,
   string? sourceFilePath,
   int sourceLineNumber)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          dimensionEnumTypes: EnumerableCE.OfTypes<TDimension1, TDimension2, TDimension3>().ToArray(),
                                          useTestMethodArgument: useTestMethodArgument,
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TDimension1 : Enum
   where TDimension2 : Enum
   where TDimension3 : Enum
{
   public static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);
   public static TDimension2 CurrentDimensionValue2 => GetCurrentDimensionValue<TDimension2>(1);
   public static TDimension3 CurrentDimensionValue3 => GetCurrentDimensionValue<TDimension3>(2);
}
