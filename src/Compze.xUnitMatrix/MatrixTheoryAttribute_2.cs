using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

public abstract class MatrixTheoryAttribute<TDimension1, TDimension2>(
   string? configurationFileName,
   string? sourceFilePath,
   int sourceLineNumber)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          dimensionEnumTypes: EnumerableCE.OfTypes<TDimension1, TDimension2>().ToArray(),
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TDimension1 : Enum
   where TDimension2 : Enum
{
   protected static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);
   protected static TDimension2 CurrentDimensionValue2 => GetCurrentDimensionValue<TDimension2>(1);
}
