using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

/// <summary>
/// Convenience base for a matrix theory attribute with two dimensions. Derive your attribute from this and read the current
/// combination's values from <see cref="CurrentDimensionValue1"/> and <see cref="CurrentDimensionValue2"/>, typically
/// re-exposed as named properties. See <see cref="MatrixTheoryAttribute"/> for the matrix model and how combinations are selected.
/// </summary>
/// <typeparam name="TDimension1">Enum type of the first dimension.</typeparam>
/// <typeparam name="TDimension2">Enum type of the second dimension.</typeparam>
/// <param name="configurationFileName">Name of the file listing which combinations to run, relative to the test assembly's output directory; <see langword="null"/> runs the full Cartesian product of all dimension values.</param>
/// <param name="sourceFilePath">Source file of the use site, normally supplied by a <see cref="System.Runtime.CompilerServices.CallerFilePathAttribute"/> parameter on the derived constructor.</param>
/// <param name="sourceLineNumber">Source line of the use site, normally supplied by a <see cref="System.Runtime.CompilerServices.CallerLineNumberAttribute"/> parameter.</param>
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
   /// <summary>The current combination's value for the first dimension (<typeparamref name="TDimension1"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);

   /// <summary>The current combination's value for the second dimension (<typeparamref name="TDimension2"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension2 CurrentDimensionValue2 => GetCurrentDimensionValue<TDimension2>(1);
}
