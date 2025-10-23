using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Compze.Utilities.SystemCE.LinqCE;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentsCombinations;
#pragma warning disable CA1813 //avoid unsealed attributes

[XunitTestCaseDiscoverer(typeof(ComponentsCombinationsTheoryDiscoverer))]
public abstract class ComponentsCombinationsTheoryAttribute :
   TheoryAttribute,
   IDataAttribute
{
   public bool UseTestMethodArgument { get; }
   readonly IReadOnlyList<Enum> _skippedComponents;
   readonly string[] _skipReasons;
   readonly Type[] _componentEnumTypes;
   readonly string _configurationFileName;

   protected ComponentsCombinationsTheoryAttribute(string configurationFileName,
                                                   Type[] componentEnumTypes,
                                                   object[]? skipped,
                                                   string[]? skipReasons,
                                                   bool useTestMethodArgument,
                                                   string? sourceFilePath,
                                                   int sourceLineNumber) : base(sourceFilePath, sourceLineNumber)
   {
      if(componentEnumTypes.Length == 0)
      {
         throw new ArgumentException($"{nameof(componentEnumTypes)} may not be empty");
      }

      UseTestMethodArgument = useTestMethodArgument;

      foreach(var type in componentEnumTypes)
      {
         if(!type.IsEnum)
            throw new ArgumentException($"Type {type.Name} must be an enum type");
      }

      skipped?.Where(it => !componentEnumTypes.Contains(it.GetType()))
              .ForEach(it => throw new ArgumentException($"{it} is not one of: {string.Join(", ", componentEnumTypes.Select(componentType => componentType.FullName))}"));

      if(skipped?.Length != skipReasons?.Length)
         throw new ArgumentException("Number of skipped components must match number of skip reasons");

      _configurationFileName = configurationFileName ?? throw new ArgumentNullException(nameof(configurationFileName));
      _componentEnumTypes = componentEnumTypes;
      _skippedComponents = skipped?.Cast<Enum>().ToList() ?? [];
      _skipReasons = skipReasons ?? [];
   }

   internal Type[] ComponentEnumTypes => _componentEnumTypes;

   SkipComponentSpecificationsCollection SkipComponentSpecifications => SkipComponentSpecificationsCollection.FromComponentsAndReasons(_skippedComponents, _skipReasons);

   bool? IDataAttribute.Explicit => Explicit;
   string? IDataAttribute.Label => null;
   string? IDataAttribute.Skip => Skip;
   Type? IDataAttribute.SkipType => SkipType;
   string? IDataAttribute.SkipUnless => SkipUnless;
   string? IDataAttribute.SkipWhen => SkipWhen;
   string? IDataAttribute.TestDisplayName => DisplayName;
   int? IDataAttribute.Timeout => Timeout > 0 ? Timeout : null;
   string[]? IDataAttribute.Traits => null;

   public ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
   {
      if(testMethod.DeclaringType != testMethod.ReflectedType) //Only run for the class that declares the test method.
      {
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow("skipped") { Skip = "Only runs in declaring class" }
            ]);
      }

      try
      {
         var combinations = ComponentsCombinationsConfigurationFileReader
                           .GetPermutations(_configurationFileName, _componentEnumTypes)
                           .Select(ITheoryDataRow (combination) => new TheoryDataRow(combination) // Pass combination object as argument
                                                                   {
                                                                      Skip = SkipComponentSpecifications.SkippedComponentFor(combination)?.ToString()
                                                                   })
                           .ToArray();
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(combinations);
      }
      catch(Exception ex)
      {
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow() { Skip = $"Failed to read configuration: {ex.Message}" }
            ]);
      }
   }

   public bool SupportsDiscoveryEnumeration() => true;
}
#pragma warning restore CA1813 //avoid unsealed attributes
