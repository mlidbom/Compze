using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

public abstract class MatrixTheoryAttribute<TDimension1, TDimension2, TDimension3, TDimension4, TDimension5>(
   string? configurationFileName,
   bool useTestMethodArgument,
   string? sourceFilePath,
   int sourceLineNumber)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          dimensionEnumTypes: EnumerableCE.OfTypes<TDimension1, TDimension2, TDimension3, TDimension4, TDimension5>().ToArray(),
                                          useTestMethodArgument: useTestMethodArgument,
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TDimension1 : Enum
   where TDimension2 : Enum
   where TDimension3 : Enum
   where TDimension4 : Enum
   where TDimension5 : Enum
{
   protected static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);
   protected static TDimension2 CurrentDimensionValue2 => GetCurrentDimensionValue<TDimension2>(1);
   protected static TDimension3 CurrentDimensionValue3 => GetCurrentDimensionValue<TDimension3>(2);
   protected static TDimension4 CurrentDimensionValue4 => GetCurrentDimensionValue<TDimension4>(3);
   protected static TDimension5 CurrentDimensionValue5 => GetCurrentDimensionValue<TDimension5>(4);
}
