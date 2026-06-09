using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

public abstract class MatrixTheoryAttribute<TDimension1>(
   string? configurationFileName,
   string? sourceFilePath,
   int sourceLineNumber)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          dimensionEnumTypes: EnumerableCE.OfTypes<TDimension1>().ToArray(),
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TDimension1 : Enum
{
   protected static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);
}
