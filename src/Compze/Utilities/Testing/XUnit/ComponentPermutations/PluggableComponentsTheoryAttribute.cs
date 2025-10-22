using System.Reflection;
using System.Runtime.CompilerServices;
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
public class PluggableComponentsTheoryAttribute :
   TheoryAttribute,
   IDataAttribute
{
   readonly IReadOnlyList<Enum> _skippedComponents;
   readonly string[] _skipReasons;
   readonly Type[] _componentEnumTypes;

   /// <summary>
   /// Pluggable Components Theory Attribute
   /// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
   /// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
   /// Use TestEnv to access the component and the information.
   /// </summary>
   public PluggableComponentsTheoryAttribute(Type[] componentEnumTypes,
                                             IReadOnlyList<Enum>? skippedComponents,
                                             string[]? skipReasons,
                                             [CallerFilePath] string? sourceFilePath = null,
                                             [CallerLineNumber] int sourceLineNumber = -1) : base(sourceFilePath, sourceLineNumber)
   {
      foreach(var type in componentEnumTypes)
      {
         if(!type.IsEnum)
            throw new ArgumentException($"Type {type.Name} must be an enum type");
      }

      _componentEnumTypes = componentEnumTypes;
      _skippedComponents = skippedComponents ?? [];
      _skipReasons = skipReasons ?? [];

      ValidateSkippedComponents();
   }

   internal Type[] ComponentEnumTypes => _componentEnumTypes;

   void ValidateSkippedComponents()
   {
      if(_skippedComponents.Count != _skipReasons.Length)
         throw new ArgumentException("Number of skipped components must match number of skip reasons");

      for(int i = 0; i < _skippedComponents.Count; i++)
      {
         var componentObj = _skippedComponents[i];
         if(componentObj == null)
            throw new ArgumentException($"Component at index {i} cannot be null");

         var componentType = componentObj.GetType();

         if(!_componentEnumTypes.Contains(componentType))
         {
            var expectedTypes = string.Join(", ", _componentEnumTypes.Select(t => t.Name));
            throw new ArgumentException(
               $"Component at index {i} must be of type {expectedTypes}, " +
               $"but was {componentType.Name}");
         }
      }
   }


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
#pragma warning disable CS0618 // Type or member is obsolete
         var permutations = GetTheoryDataRowsInternal();
#pragma warning restore CS0618 // Type or member is obsolete

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

   [Obsolete("Only for internal use")]
   public ITheoryDataRow[] GetTheoryDataRowsInternal()
   {
      if(_componentEnumTypes == null || _componentEnumTypes.Length == 0)
      {
         throw new InvalidOperationException("TypedPCTAttribute.ComponentTypes is null or empty!");
      }

      var permutations = PluggableComponentsReader.GetPermutations(_componentEnumTypes);

      return permutations
            .Select(ITheoryDataRow (permutation) => new TheoryDataRow(permutation.ToString()) // Pass permutation string as argument
                                                    {
                                                       Skip = SkippedComponents.SkippedComponentFor(permutation)?.ToString()
                                                    })
            .ToArray();
   }

   public bool SupportsDiscoveryEnumeration() => true; // Yes, we can enumerate at discovery time
}
#pragma warning restore CA1813 //avoid unsealed attributes
