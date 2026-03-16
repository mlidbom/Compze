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
   Type[] componentEnumTypes,
   bool useTestMethodArgument,
   string? sourceFilePath = null,
   int sourceLineNumber = -1)
   :
      TheoryAttribute(sourceFilePath, sourceLineNumber),
      IDataAttribute
{
   internal bool UseTestMethodArgument { get; } = useTestMethodArgument;

   readonly Type[] _componentEnumTypes = componentEnumTypes;
   readonly string? _configurationFileName = configurationFileName;
   readonly List<ComponentSkipSpecification> _subclassSkipSpecifications = [];

   /// <summary>
   /// Allows subclasses to skip dimension values in their constructors.
   /// The enum type is preserved because it flows through a typed C# method call, not IL metadata.
   /// </summary>
   protected void SkipValues<TDimension>(TDimension value, string reason)
      where TDimension : struct, Enum
   {
      _subclassSkipSpecifications.Add(new ComponentSkipSpecification(value, reason));
   }

   /// <inheritdoc cref="SkipValues{TDimension}(TDimension, string)"/>
   protected void SkipValues<TDimension>(TDimension[] values, string reason)
      where TDimension : struct, Enum
   {
      foreach(var value in values)
         _subclassSkipSpecifications.Add(new ComponentSkipSpecification(value, reason));
   }

   string? ValidateConfiguration()
   {
      if(_componentEnumTypes.Length == 0)
         return "Component enum types may not be empty";

      foreach(var type in _componentEnumTypes)
      {
         if(!type.IsEnum)
            return $"Type {type.Name} must be an enum type";
      }

      return null;
   }

   string? ValidateSkipSpecifications(IReadOnlyList<ComponentSkipSpecification> skipSpecifications)
   {
      var invalidComponent = skipSpecifications.FirstOrDefault(it => !_componentEnumTypes.Contains(it.ComponentType));
      if(invalidComponent != null)
         return $"Skipped component {invalidComponent.ComponentValue} (type {invalidComponent.ComponentType.Name}) is not one of the configured component types: {string.Join(", ", _componentEnumTypes.Select(t => t.Name))}";

      return null;
   }

   static IReadOnlyList<ComponentSkipSpecification> CollectSkipAttributesFromMethod(MethodInfo testMethod)
   {
      var skipAttributeType = typeof(SkipAttribute<>);
      return testMethod.GetCustomAttributes(inherit: true)
                       .Where(attr => attr.GetType().IsGenericType && attr.GetType().GetGenericTypeDefinition() == skipAttributeType)
                       .SelectMany(attr =>
                        {
                           var attrType = attr.GetType();
                           var values = (Array)attrType.GetProperty(nameof(SkipAttribute<DayOfWeek>.Values))!.GetValue(attr)!;
                           var reason = (string)attrType.GetProperty(nameof(SkipAttribute<DayOfWeek>.Reason))!.GetValue(attr)!;
                           return values.Cast<Enum>().Select(value => new ComponentSkipSpecification(value, reason));
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

   public ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
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
                                                                      Skip = matrixSkipSpecification.SkippedComponentFor(combination)?.ToString()
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
         ? MatrixConfigurationFileReader.GetCombinations(_configurationFileName, _componentEnumTypes)
         : AllCombinationsFromEnumTypes(_componentEnumTypes);

   static IReadOnlyList<MatrixCombination> AllCombinationsFromEnumTypes(Type[] componentEnumTypes)
   {
      var allEnumValues = componentEnumTypes
                         .Select(type => Enum.GetValues(type).Cast<Enum>().ToArray() as IReadOnlyList<Enum>)
                         .ToList();

      return allEnumValues
            .CartesianProduct()
            .Select(MatrixCombination.FromComponentEnumValues)
            .ToList();
   }

   public bool SupportsDiscoveryEnumeration() => true;

   protected static TComponent GetCurrentComponent<TComponent>(int index) where TComponent : Enum
   {
      var combination = MatrixCombination.Current;
      if(index >= combination.Components.Count)
         throw new InvalidOperationException($"The current test combination has {combination.Components.Count} component(s), but component at index {index} ({typeof(TComponent).Name}) was requested.");

      var component = combination.Components[index];
      if(component is not TComponent typed)
         throw new InvalidOperationException($"Expected component at index {index} to be {typeof(TComponent).Name}, but found {component.GetType().Name}.");

      return typed;
   }
}
