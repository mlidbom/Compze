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
[XunitTestCaseDiscoverer(typeof(PluggableComponentsTheoryDiscoverer))] // Use standard TheoryDiscoverer!
public class PluggableComponentsTheoryAttribute(
   Type[]? componentEnumTypes = null,
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
   TheoryAttribute(sourceFilePath, sourceLineNumber),
   IDataAttribute
{
   string[] _skipped = [];
   readonly Type[] _componentEnumTypes = componentEnumTypes ?? [];

   // Debug: Output types in constructor
   void _() => Console.WriteLine($"[Constructor] Types: {(_componentEnumTypes.Length > 0 ? string.Join(", ", _componentEnumTypes.Select(t => t.Name)) : "EMPTY")}");

   /// <summary>
   /// Gets the component enum types for this attribute, if any.
   /// </summary>
   public Type[] ComponentEnumTypes => _componentEnumTypes;

   /// <summary>
   /// For type-safe derived classes: validates and converts enum components to skip specifications.
   /// </summary>
   /// <param name="skippedComponents">Array of enum values to skip (object[] due to attribute limitations, but must contain Enum values)</param>
   /// <param name="skipReasons">Corresponding reasons for skipping</param>
   protected void InitializeTypedSkipped(object[]? skippedComponents, string[]? skipReasons)
   {
      // If no components to skip, do nothing
      if(skippedComponents == null || skipReasons == null || skippedComponents.Length == 0)
         return;

      if(skippedComponents.Length != skipReasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      // Validate all types are enums
      foreach(var type in _componentEnumTypes)
      {
         if(!type.IsEnum)
            throw new ArgumentException($"Type {type.Name} must be an enum type");
      }

      var skipped = new List<string>();

      for(int i = 0; i < skippedComponents.Length; i++)
      {
         var componentObj = skippedComponents[i];
         if(componentObj == null)
            throw new ArgumentException($"Component at index {i} cannot be null");

         // Cast to Enum - we know it must be an enum
         if(componentObj is not Enum componentEnum)
            throw new ArgumentException($"Component at index {i} must be an Enum, but was {componentObj.GetType().Name}");

         var componentType = componentEnum.GetType();

         // Check if this component is one of our expected enum types
         if(!_componentEnumTypes.Contains(componentType))
         {
            var expectedTypes = string.Join(", ", _componentEnumTypes.Select(t => t.Name));
            throw new ArgumentException(
               $"Component at index {i} must be of type {expectedTypes}, " +
               $"but was {componentType.Name}");
         }

         // ComponentSkipSpecification.Skip is a generic method, can't call it dynamically easily
         // Just format the string directly: "EnumValue::Reason"
         var skipSpec = $"{componentEnum}::{skipReasons[i]}";
         skipped.Add(skipSpec);
      }

      _skipped = [..skipped];
   }

   /// <summary>
   /// Components to exclude from test execution.
   /// Format: "ComponentName::Reason" (reason is mandatory)
   /// Example: ["Type1Component1::Not implemented yet"]
   /// </summary>
   public string[] Skipped
   {
      get => _skipped;
      init => _skipped = value;
   }

   ExclusionsCollection SkippedComponents => ExclusionsCollection.Parse(Skipped);

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
      // DEBUG: What type is this?
      var thisType = GetType();

      // Always use TypedPCT component types - it's the only supported attribute now
      var componentTypes = TypedPCTAttribute.ComponentTypes;

      if(componentTypes == null || componentTypes.Length == 0)
      {
         throw new InvalidOperationException("TypedPCTAttribute.ComponentTypes is null or empty!");
      }

      // Read permutations from file with type information - components are parsed as enums
      var permutations = PluggableComponentsReader.GetPermutations(componentTypes);

      return permutations
            .Select(ITheoryDataRow (permutation) =>
             {
                var exclusion = SkippedComponents.FindMatchingExclusion(permutation);
                var permString = permutation.ToString();

                // TheoryDataRow needs DATA (the arguments), not just display name
                return new TheoryDataRow(permString) // Pass permutation string as argument
                       {
                          Skip = exclusion != null ? $"{exclusion.ComponentName}: {exclusion.Reason}" : null
                       };
             })
            .ToArray();
   }

   public bool SupportsDiscoveryEnumeration() => true; // Yes, we can enumerate at discovery time
}
#pragma warning restore CA1813 //avoid unsealed attributes
