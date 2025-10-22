using System.Reflection;
using System.Runtime.CompilerServices;
using Compze.Utilities.SystemCE.LinqCE;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// ReSharper disable ExplicitCallerInfoArgument

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;
#pragma warning disable CA1813 //avoid unsealed attributes

// ReSharper disable GrammarMistakeInComment
/// <summary>
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))]
public abstract class PluggableComponentsTheoryAttribute :
   TheoryAttribute,
   IDataAttribute
{
   readonly IReadOnlyList<Enum> _skippedComponents;
   readonly string[] _skipReasons;
   readonly Type[] _componentEnumTypes;
   readonly string _configurationFileName;

   /// <summary>
   /// Pluggable Components Theory Attribute
   /// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
   /// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
   /// Use TestEnv to access the component and the information.
   /// </summary>
   protected PluggableComponentsTheoryAttribute(string configurationFileName,
                                                Type[] componentEnumTypes,
                                                object[]? skippedComponents,
                                                string[]? skipReasons,
                                                [CallerFilePath] string? sourceFilePath = null,
                                                [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
   {
      if(componentEnumTypes.Length == 0)
      {
         throw new ArgumentException($"{nameof(componentEnumTypes)} may not be empty");
      }

      foreach(var type in componentEnumTypes)
      {
         if(!type.IsEnum)
            throw new ArgumentException($"Type {type.Name} must be an enum type");
      }

      skippedComponents?.Where(it => !componentEnumTypes.Contains(it.GetType()))
                        .ForEach(it => throw new ArgumentException($"{it} is not one of: {string.Join(", ", componentEnumTypes.Select(componentType => componentType.FullName))}"));

      if(skippedComponents?.Length != skipReasons?.Length)
         throw new ArgumentException("Number of skipped components must match number of skip reasons");

      _configurationFileName = configurationFileName ?? throw new ArgumentNullException(nameof(configurationFileName));
      _componentEnumTypes = componentEnumTypes;
      _skippedComponents = skippedComponents?.Cast<Enum>().ToList() ?? [];
      _skipReasons = skipReasons ?? [];
   }

   internal Type[] ComponentEnumTypes => _componentEnumTypes;
   internal string ConfigurationFileName => _configurationFileName;

   SkippedComponentsCollection SkippedComponents => SkippedComponentsCollection.FromComponentsAndReasons(_skippedComponents, _skipReasons);

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
         var permutations = ComponentPermutationsConfigurationFileReader.GetPermutations(_configurationFileName, _componentEnumTypes)
                                                     .Select(ITheoryDataRow (permutation) => new TheoryDataRow(permutation.ToString()) // Pass permutation string as argument
                                                                                             {
                                                                                                Skip = SkippedComponents.SkippedComponentFor(permutation)?.ToString()
                                                                                             })
                                                     .ToArray();
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(permutations);
      }
      catch(ArgumentException ex)
      {
         // Validation error - return a single skipped test with the error message
         return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(
            [
               new TheoryDataRow() { Skip = ex.Message }
            ]);
      }
   }

   public bool SupportsDiscoveryEnumeration() => true; // Yes, we can enumerate at discovery time
}
#pragma warning restore CA1813 //avoid unsealed attributes
