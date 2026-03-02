using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

[XunitTestCaseDiscoverer(typeof(ComponentCombinationsTheoryDiscoverer))]
public abstract class ComponentCombinationsTheoryAttribute(
   string configurationFileName,
   Type[] componentEnumTypes,
   bool useTestMethodArgument,
   string? sourceFilePath = null,
   int sourceLineNumber = -1)
   :
      TheoryAttribute(sourceFilePath, sourceLineNumber),
      IDataAttribute
{
   internal bool UseTestMethodArgument { get; } = useTestMethodArgument;
   public object[]? Skipped { get; init; }
   public string[]? SkipReasons { get; init; }
   public Type? OnlyConsider { get; init; }

   readonly Type[] _componentEnumTypes = componentEnumTypes;
   readonly string _configurationFileName = configurationFileName;

   string? ValidateConfiguration()
   {
      if(_componentEnumTypes.Length == 0)
         return "Component enum types may not be empty";

      foreach(var type in _componentEnumTypes)
      {
         if(!type.IsEnum)
            return $"Type {type.Name} must be an enum type";
      }

      if(Skipped != null)
      {
         var invalidComponent = Skipped.FirstOrDefault(it => !_componentEnumTypes.Contains(it.GetType()));
         if(invalidComponent != null)
            return $"{invalidComponent} is not one of: {string.Join(", ", _componentEnumTypes.Select(componentType => componentType.FullName))}";
      }

      if(Skipped?.Length != SkipReasons?.Length)
         return "Number of skipped components must match number of skip reasons";

      if(OnlyConsider != null && !_componentEnumTypes.Contains(OnlyConsider))
         return $"{nameof(OnlyConsider)} is not one of the component types";

      return null;
   }

   IReadOnlyList<Enum> SkippedComponents => Skipped?.Cast<Enum>().ToList() ?? [];

   SkipComponentSpecificationsCollection SkipComponentSpecifications => SkipComponentSpecificationsCollection.FromComponentsAndReasons(SkippedComponents, SkipReasons ?? []);

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
         var combinations = ComponentCombinationsConfigurationFileReader
                           .GetCombinations(_configurationFileName, _componentEnumTypes, OnlyConsiderComponentIndex)
                           .Select(ITheoryDataRow (combination) => new TheoryDataRow(combination) // Pass combination object as argument
                                                                   {
                                                                      Skip = SkipComponentSpecifications.SkippedComponentFor(combination)?.ToString()
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

   int? OnlyConsiderComponentIndex => OnlyConsider == null
                                         ? null
                                         : _componentEnumTypes.ToList().IndexOf(OnlyConsider);

   public bool SupportsDiscoveryEnumeration() => true;
}
