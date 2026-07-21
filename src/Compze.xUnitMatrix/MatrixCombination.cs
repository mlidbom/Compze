using Compze.Internals.SystemCE;
using System.ComponentModel;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;
using Compze.xUnitMatrix._private;

namespace Compze.xUnitMatrix;

/// <summary>
/// One cell of the test matrix: a single value chosen for each dimension. Available while a matrix test runs via
/// <see cref="Current"/>, in both the test class constructor and the test method. The typed
/// <c>CurrentDimensionValue1</c>…<c>CurrentDimensionValueN</c> properties on the generic
/// <see cref="MatrixTheoryAttribute{TDimension1}"/> bases are the recommended way to read it; use
/// <see cref="DimensionValues"/> directly for lower-level untyped access — for more than five dimensions, or to write your
/// own accessor extension methods over the combination.
/// </summary>
public sealed class MatrixCombination : IXunitSerializable
{
   /// <summary>
   /// The combination the currently executing matrix test is running against — available within the test class constructor
   /// and the test method of a test driven by a <see cref="MatrixTheoryAttribute"/> subclass.
   /// </summary>
   /// <remarks>
   /// Accessing this outside a running matrix test, or from a different async context than the one the test runs on, throws.
   /// That is a usage error with no recoverable handling, not a condition to catch.
   /// </remarks>
#pragma warning disable CA1065 //throwing in a property.
   public static MatrixCombination Current => TryGetCurrent() ?? throw new NoCurrentMatrixCombinationException();
#pragma warning restore CA1065

   static MatrixCombination? TryGetCurrent() => CurrentInternal.Value?.Value;

   /// <summary>
   /// The chosen value for each dimension, in dimension order: <c>DimensionValues[i]</c> is a value of the i-th dimension's
   /// enum type, matching the order of the type parameters (or enum types) the matrix attribute was declared with.
   /// </summary>
   public IReadOnlyList<Enum> DimensionValues { get; private set; }

   /// <summary>The dimension values joined by '<c>:</c>' — the form shown in test display names and used in configuration files.</summary>
   public override string ToString() => string.Join(Separator, DimensionValues.Select(it => it.ToString()));

   internal const string Separator = ":";

   /// <summary>Infrastructure only: the parameterless constructor xUnit's deserializer requires. Do not call it.</summary>
   [Obsolete("Called by xUnit deserializer", error: true)]
   // ReSharper disable once UnusedMember.Global
   public MatrixCombination() => DimensionValues = [];

   MatrixCombination(IEnumerable<Enum> dimensionValues) => DimensionValues = dimensionValues.ToList();

   void IXunitSerializable.Serialize(IXunitSerializationInfo info)
   {
      info.AddValue("DimensionValueNames", DimensionValues.Select(it => it.ToString()).ToArray());
      info.AddValue("DimensionEnumTypes", DimensionValues.Select(it => it.GetType().AssemblyQualifiedName!).ToArray());
   }

   void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
   {
      var dimensionValueNames = info.GetValue<string[]>("DimensionValueNames") ?? throw new InvalidEnumArgumentException("DimensionValueNames is null");
      var dimensionEnumTypes = (info.GetValue<string[]>("DimensionEnumTypes") ?? throw new InvalidEnumArgumentException("DimensionEnumTypes is null"))
                          .Select(it => Type.GetType(it, throwOnError: true)!)
                          .ToArray();

      var combination = FromDimensionValueNames(dimensionValueNames, dimensionEnumTypes);
      DimensionValues = combination.DimensionValues;
   }

   internal static MatrixCombination FromDimensionValues(IEnumerable<Enum> dimensionValues) => new(dimensionValues);

   static MatrixCombination FromDimensionValueNames(IReadOnlyList<string> dimensionValueNames, Type[] dimensionEnumTypes)
   {
      if(dimensionValueNames.Count != dimensionEnumTypes.Length)
         throw new ArgumentException($"Dimension values: [{string.Join(", ", dimensionValueNames)}] do not match the specified dimension enum types [{string.Join(", ", dimensionEnumTypes.Select(it => it.Name))}]");

      return new MatrixCombination(dimensionValueNames.Zip(dimensionEnumTypes, NameToEnum).ToList());
   }

   static Enum NameToEnum(string dimensionValueName, Type dimensionEnumType)
   {
      try
      {
         return (Enum)Enum.Parse(dimensionEnumType, dimensionValueName);
      }
      catch(ArgumentException ex)
      {
         throw new ArgumentException($"Invalid dimension value '{dimensionValueName}' for dimension enum type {dimensionEnumType}", ex);
      }
   }

   static readonly AsyncLocal<LazyCE<MatrixCombination>?> CurrentInternal = new();

   internal static async Task<TReturn> RunInContextAsync<TReturn>(LazyCE<MatrixCombination> combination, Func<Task<TReturn>> executeTest)
   {
      CurrentInternal.Value = combination;
      try
      {
         return await executeTest().caf();
      }
      finally
      {
         CurrentInternal.Value = null;
      }
   }
}
