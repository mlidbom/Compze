using System.Reflection;
using Compze.Internals.SystemCE.LinqCE;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;
using Compze.xUnitMatrix._private;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

/// <summary>
/// Base class for an xUnit v3 theory attribute that runs a test once for every combination of a set of pluggable-component
/// dimensions. A dimension is an <see langword="enum"/> type and each of its members is a dimension value (a persistence
/// layer, a DI container, a serializer, …); a combination picks one value per dimension. The test runs once per combination,
/// with the combination shown in its display name.
/// </summary>
/// <remarks>
/// <para>
/// Derive your attribute from one of the generic convenience bases <see cref="MatrixTheoryAttribute{TDimension1}"/> through
/// <c>MatrixTheoryAttribute&lt;T1, T2, T3, T4, T5&gt;</c>; they expose the current combination's values as the type-safe
/// <c>CurrentDimensionValue1</c>…<c>CurrentDimensionValueN</c> properties. Derive from this non-generic base directly only
/// for more than five dimensions: pass the dimension enum types to <paramref name="dimensionEnumTypes"/> and read each value
/// with <see cref="GetCurrentDimensionValue{TDimension}"/>.
/// </para>
/// <para>
/// While a test runs, the active combination is available from <see cref="MatrixCombination.Current"/> in both the test
/// class constructor and the test method. The test method takes no parameters of its own.
/// </para>
/// <para>
/// <paramref name="configurationFileName"/> selects which combinations run: <see langword="null"/> runs the full Cartesian
/// product of every enum value; a file name runs only the combinations listed in that file (one per line, with <c>*</c>
/// wildcards — see the package README). To keep an unsupported combination visible in the runner but un-run, mark the test
/// with <see cref="SkipAttribute{TDimension}"/>.
/// </para>
/// </remarks>
/// <param name="configurationFileName">
/// Name of the file listing which combinations to run, resolved relative to the test assembly's output directory; or
/// <see langword="null"/> to run the full Cartesian product of all dimension values.
/// </param>
/// <param name="dimensionEnumTypes">
/// The enum types defining the matrix dimensions, in order. <see cref="MatrixCombination.DimensionValues"/> element <c>i</c>
/// holds a value of the enum type at index <c>i</c>.
/// </param>
/// <param name="sourceFilePath">
/// Source file of the attribute's use site, normally supplied by a
/// <see cref="System.Runtime.CompilerServices.CallerFilePathAttribute"/> parameter on the derived attribute's constructor.
/// Forwarded to xUnit for test source location.
/// </param>
/// <param name="sourceLineNumber">
/// Source line of the attribute's use site, normally supplied by a
/// <see cref="System.Runtime.CompilerServices.CallerLineNumberAttribute"/> parameter, as with <paramref name="sourceFilePath"/>.
/// </param>
[XunitTestCaseDiscoverer(typeof(MatrixTheoryDiscoverer))]
public abstract class MatrixTheoryAttribute(
   string? configurationFileName,
   Type[] dimensionEnumTypes,
   string? sourceFilePath,
   int sourceLineNumber)
   :
      TheoryAttribute(sourceFilePath, sourceLineNumber),
      IDataAttribute
{
   readonly Type[] _dimensionEnumTypes = dimensionEnumTypes;
   readonly string? _configurationFileName = configurationFileName;
   string? ValidateConfiguration()
   {
      if(_dimensionEnumTypes.Length == 0)
         return "Dimension enum types may not be empty";

      foreach(var type in _dimensionEnumTypes)
      {
         if(!type.IsEnum)
            return $"Type {type.Name} must be an enum type";
      }

      return null;
   }

   string? ValidateSkipSpecifications(IReadOnlyList<DimensionValueSkipSpecification> skipSpecifications)
   {
      var invalidSkip = skipSpecifications.FirstOrDefault(it => !_dimensionEnumTypes.Contains(it.DimensionEnumType));
      if(invalidSkip != null)
         return $"Skipped dimension value {invalidSkip.DimensionValue} (type {invalidSkip.DimensionEnumType.Name}) is not one of the configured dimension enum types: {string.Join(", ", _dimensionEnumTypes.Select(t => t.Name))}";

      return null;
   }

   static IReadOnlyList<DimensionValueSkipSpecification> CollectSkipAttributesFromMethod(MethodInfo testMethod)
   {
      var skipAttributeType = typeof(SkipAttribute<>);
      return testMethod.GetCustomAttributes(inherit: true)
                       .Where(attr => attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == skipAttributeType)
                       .SelectMany(attr =>
                        {
                           var attrType = attr.GetType();
                           const BindingFlags skipPropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                           var values = (Array)attrType.GetProperty(nameof(SkipAttribute<>.SkippedDimensionValues), skipPropertyFlags)!.GetValue(attr)!;
                           var reason = (string)attrType.GetProperty(nameof(SkipAttribute<>.Reason), skipPropertyFlags)!.GetValue(attr)!;
                           return values.Cast<Enum>().Select(value => new DimensionValueSkipSpecification(value, reason));
                        })
                       .ToList();
   }

#pragma warning disable CA1033 // The explicit IDataAttribute implementation is deliberate: it keeps xUnit's discovery plumbing (GetData, SupportsDiscoveryEnumeration, Skip, ...) off the subclass-facing API surface, and members like Skip would otherwise collide with the inherited TheoryAttribute members.
   bool? IDataAttribute.Explicit => Explicit;
   string? IDataAttribute.Label => null;
   string? IDataAttribute.Skip => Skip;
   Type? IDataAttribute.SkipType => SkipType;
   string? IDataAttribute.SkipUnless => SkipUnless;
   string? IDataAttribute.SkipWhen => SkipWhen;
   string? IDataAttribute.TestDisplayName => DisplayName;
   int? IDataAttribute.Timeout => Timeout > 0 ? Timeout : null;
   string[]? IDataAttribute.Traits => null;

   ValueTask<IReadOnlyCollection<ITheoryDataRow>> IDataAttribute.GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
   {
      if(testMethod.DeclaringType != testMethod.ReflectedType) //Only run for the class that declares the test method.
      {
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow("skipped") { Skip = "Only runs in declaring class" }
            ]);
      }

      // Validate configuration first
      var validationError = ValidateConfiguration();
      if(validationError != null)
      {
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow() { Skip = $"Invalid attribute configuration: {validationError}" }
            ]);
      }

      try
      {
         var skipSpecifications = CollectSkipAttributesFromMethod(testMethod);

         var skipValidationError = ValidateSkipSpecifications(skipSpecifications);
         if(skipValidationError != null)
         {
            return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
               [
                  new TheoryDataRow() { Skip = $"Invalid skip configuration: {skipValidationError}" }
               ]);
         }

         var matrixSkipSpecification = new MatrixSkipSpecification(skipSpecifications);
         var combinations = GetCombinations()
                           .Select(ITheoryDataRow (combination) => new TheoryDataRow(combination) // Pass combination object as argument
                                                                   {
                                                                      Skip = matrixSkipSpecification.SkippedDimensionValueFor(combination)?.ToString()
                                                                   })
                           .ToArray();
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(combinations);
      }
#pragma warning disable CA1031 //This is the best way we've found for surfacing the failure in a way that is actually displayed in test runners
      catch(Exception ex)
      {
#pragma warning restore CA1031
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow() { Skip = $"Failed to read configuration: {ex.Message}" }
            ]);
      }
   }

   IReadOnlyList<MatrixCombination> GetCombinations() =>
      _configurationFileName != null
         ? MatrixConfigurationFileReader.GetCombinations(_configurationFileName, _dimensionEnumTypes)
         : AllCombinationsFromEnumTypes(_dimensionEnumTypes);

   static IReadOnlyList<MatrixCombination> AllCombinationsFromEnumTypes(Type[] dimensionEnumTypes)
   {
      var allEnumValues = dimensionEnumTypes
                         .Select(type => Enum.GetValues(type).Cast<Enum>().ToArray() as IReadOnlyList<Enum>)
                         .ToList();

      return allEnumValues
            .CartesianProduct()
            .Select(MatrixCombination.FromDimensionValues)
            .ToList();
   }

   bool IDataAttribute.SupportsDiscoveryEnumeration() => true;
#pragma warning restore CA1033 // explicit IDataAttribute implementation is deliberate (see disable above)

   /// <summary>
   /// Returns <see cref="MatrixCombination.Current"/>'s value for the dimension at <paramref name="dimensionIndex"/>, typed
   /// as <typeparamref name="TDimension"/>. This is the primitive the generic convenience bases build their
   /// <c>CurrentDimensionValue1</c>…<c>CurrentDimensionValueN</c> properties on; call it directly only when deriving from the
   /// non-generic <see cref="MatrixTheoryAttribute"/> for more than five dimensions. Like <see cref="MatrixCombination.Current"/>,
   /// it must be called while a matrix test is executing.
   /// </summary>
   /// <typeparam name="TDimension">Enum type of the requested dimension; must match the dimension at <paramref name="dimensionIndex"/>.</typeparam>
   /// <param name="dimensionIndex">Zero-based index of the dimension, in the order the dimensions were declared.</param>
   /// <returns>The current combination's value for that dimension.</returns>
   /// <exception cref="System.InvalidOperationException">
   /// <paramref name="dimensionIndex"/> is out of range for the current combination, or the dimension at that index is not of
   /// type <typeparamref name="TDimension"/>.
   /// </exception>
   protected static TDimension GetCurrentDimensionValue<TDimension>(int dimensionIndex) where TDimension : Enum
   {
      var combination = MatrixCombination.Current;
      if(dimensionIndex >= combination.DimensionValues.Count)
         throw new InvalidOperationException($"The current test combination has {combination.DimensionValues.Count} dimension value(s), but the dimension value at index {dimensionIndex} ({typeof(TDimension).Name}) was requested.");

      var dimensionValue = combination.DimensionValues[dimensionIndex];
      if(dimensionValue is not TDimension typed)
         throw new InvalidOperationException($"Expected the dimension value at index {dimensionIndex} to be {typeof(TDimension).Name}, but found {dimensionValue.GetType().Name}.");

      return typed;
   }
}
