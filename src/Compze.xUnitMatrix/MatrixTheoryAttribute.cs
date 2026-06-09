using System.Reflection;
using Compze.Internals.SystemCE.LinqCE;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.xUnitMatrix;

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
   readonly List<DimensionValueSkipSpecification> _subclassSkipSpecifications = [];

   /// <summary>
   /// Allows subclasses to skip dimension values in their constructors.
   /// The enum type is preserved because it flows through a typed C# method call, not IL metadata.
   /// </summary>
   protected void SkipValues<TDimension>(TDimension value, string reason)
      where TDimension : struct, Enum
   {
      _subclassSkipSpecifications.Add(new DimensionValueSkipSpecification(value, reason));
   }

   /// <inheritdoc cref="SkipValues{TDimension}(TDimension, string)"/>
   protected void SkipValues<TDimension>(TDimension[] values, string reason)
      where TDimension : struct, Enum
   {
      foreach(var value in values)
         _subclassSkipSpecifications.Add(new DimensionValueSkipSpecification(value, reason));
   }

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

#pragma warning disable CA1033 // Interface methods should be callable by child types. We can't, that would hide the base class methods
   bool? IDataAttribute.Explicit => Explicit;
   string? IDataAttribute.Label => null;
   string? IDataAttribute.Skip => Skip;
   Type? IDataAttribute.SkipType => SkipType;
   string? IDataAttribute.SkipUnless => SkipUnless;
   string? IDataAttribute.SkipWhen => SkipWhen;
   string? IDataAttribute.TestDisplayName => DisplayName;
   int? IDataAttribute.Timeout => Timeout > 0 ? Timeout : null;
   string[]? IDataAttribute.Traits => null;
#pragma warning restore CA1033 // Interface methods should be callable by child types. We can't, that would hide the base class methods

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
         var allSkipSpecifications = _subclassSkipSpecifications
                                    .Concat(CollectSkipAttributesFromMethod(testMethod))
                                    .ToList();

         var skipValidationError = ValidateSkipSpecifications(allSkipSpecifications);
         if(skipValidationError != null)
         {
            return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
               [
                  new TheoryDataRow() { Skip = $"Invalid skip configuration: {skipValidationError}" }
               ]);
         }

         var matrixSkipSpecification = new MatrixSkipSpecification(allSkipSpecifications);
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
