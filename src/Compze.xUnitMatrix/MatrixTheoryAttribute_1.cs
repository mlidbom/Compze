using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

public abstract class MatrixTheoryAttribute<TDimension1>(
   string? configurationFileName,
   bool useTestMethodArgument,
   string? sourceFilePath = null,
   int sourceLineNumber = -1)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          dimensionEnumTypes: EnumerableCE.OfTypes<TDimension1>().ToArray(),
                                          useTestMethodArgument: useTestMethodArgument,
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TDimension1 : Enum
{
   public static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);
}
