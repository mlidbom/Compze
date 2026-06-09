using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

/// <summary>
/// Convenience base for a matrix theory attribute with five dimensions. Derive your attribute from this and read the
/// current combination's values from <see cref="CurrentDimensionValue1"/> through <see cref="CurrentDimensionValue5"/>,
/// typically re-exposed as named properties. See <see cref="MatrixTheoryAttribute"/> for the matrix model and how combinations are selected.
/// </summary>
/// <typeparam name="TDimension1">Enum type of the first dimension.</typeparam>
/// <typeparam name="TDimension2">Enum type of the second dimension.</typeparam>
/// <typeparam name="TDimension3">Enum type of the third dimension.</typeparam>
/// <typeparam name="TDimension4">Enum type of the fourth dimension.</typeparam>
/// <typeparam name="TDimension5">Enum type of the fifth dimension.</typeparam>
/// <param name="configurationFileName">Name of the file listing which combinations to run, relative to the test assembly's output directory; <see langword="null"/> runs the full Cartesian product of all dimension values.</param>
/// <param name="sourceFilePath">Source file of the use site, normally supplied by a <see cref="System.Runtime.CompilerServices.CallerFilePathAttribute"/> parameter on the derived constructor.</param>
/// <param name="sourceLineNumber">Source line of the use site, normally supplied by a <see cref="System.Runtime.CompilerServices.CallerLineNumberAttribute"/> parameter.</param>
public abstract class MatrixTheoryAttribute<TDimension1, TDimension2, TDimension3, TDimension4, TDimension5>(
   string? configurationFileName,
   string? sourceFilePath,
   int sourceLineNumber)
   : MatrixTheoryAttribute(configurationFileName: configurationFileName,
                                          dimensionEnumTypes: EnumerableCE.OfTypes<TDimension1, TDimension2, TDimension3, TDimension4, TDimension5>().ToArray(),
                                          sourceFilePath: sourceFilePath,
                                          sourceLineNumber: sourceLineNumber)
   where TDimension1 : Enum
   where TDimension2 : Enum
   where TDimension3 : Enum
   where TDimension4 : Enum
   where TDimension5 : Enum
{
   /// <summary>The current combination's value for the first dimension (<typeparamref name="TDimension1"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension1 CurrentDimensionValue1 => GetCurrentDimensionValue<TDimension1>(0);

   /// <summary>The current combination's value for the second dimension (<typeparamref name="TDimension2"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension2 CurrentDimensionValue2 => GetCurrentDimensionValue<TDimension2>(1);

   /// <summary>The current combination's value for the third dimension (<typeparamref name="TDimension3"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension3 CurrentDimensionValue3 => GetCurrentDimensionValue<TDimension3>(2);

   /// <summary>The current combination's value for the fourth dimension (<typeparamref name="TDimension4"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension4 CurrentDimensionValue4 => GetCurrentDimensionValue<TDimension4>(3);

   /// <summary>The current combination's value for the fifth dimension (<typeparamref name="TDimension5"/>). Re-expose as a named property for readable test code.</summary>
   protected static TDimension5 CurrentDimensionValue5 => GetCurrentDimensionValue<TDimension5>(4);
}
