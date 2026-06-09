using Compze.Internals.SystemCE;
using System.ComponentModel;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Xunit.Sdk;

namespace Compze.xUnitMatrix;

public sealed class MatrixCombination : IXunitSerializable
{
#pragma warning disable CA1065 //throwing in a property.
   public static MatrixCombination Current => TryGetCurrent() ?? throw new NoCurrentMatrixCombinationException();
#pragma warning restore CA1065

   static MatrixCombination? TryGetCurrent() => CurrentInternal.Value?.Value;

   public IReadOnlyList<Enum> DimensionValues { get; private set; }

   public override string ToString() => string.Join(Separator, DimensionValues.Select(it => it.ToString()));

   internal const string Separator = ":";

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

///<summary>
/// Thrown when <see cref="MatrixCombination.Current"/> is accessed while no matrix test is running on the current async context.
/// <see cref="MatrixCombination.Current"/> is only available within the test class constructor or test method of a test driven by
/// a <see cref="MatrixTheoryAttribute"/> subclass; reaching it from anywhere else — or from a different async context than the
/// one the test runs on — is a usage error, not a recoverable condition.
///</summary>
class NoCurrentMatrixCombinationException() : Exception(
   $"There is no current {nameof(MatrixCombination)} on this async context. {nameof(MatrixCombination)}.{nameof(MatrixCombination.Current)} is only available "
 + $"while a matrix test is executing: within the test class constructor or the test method of a test marked with a {nameof(MatrixTheoryAttribute)} subclass.");
