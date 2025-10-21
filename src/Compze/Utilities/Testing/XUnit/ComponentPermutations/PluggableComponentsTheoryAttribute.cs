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
   [CallerFilePath] string? sourceFilePath = null,
   [CallerLineNumber] int sourceLineNumber = -1) :
   TheoryAttribute(sourceFilePath, sourceLineNumber),
   IDataAttribute
{
   string[] _skipped = [];

   /// <summary>
   /// For type-safe derived classes: validates and converts enum components to skip specifications.
   /// </summary>
   /// <param name="componentEnumTypes">Array of enum types that are valid for this attribute</param>
   /// <param name="skippedComponents">Array of enum values to skip</param>
   /// <param name="skipReasons">Corresponding reasons for skipping</param>
   protected void InitializeTypedSkipped(Type[] componentEnumTypes, object[]? skippedComponents, string[]? skipReasons)
   {
      if(skippedComponents == null || skipReasons == null)
      {
         _skipped = [];
         return;
      }

      if(skippedComponents.Length != skipReasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      // Validate all types are enums
      foreach(var type in componentEnumTypes)
      {
         if(!type.IsEnum)
            throw new ArgumentException($"Type {type.Name} must be an enum type");
      }

      var skipped = new List<string>();

      for(int i = 0; i < skippedComponents.Length; i++)
      {
         var component = skippedComponents[i];
         if(component == null)
            throw new ArgumentException($"Component at index {i} cannot be null");

         var componentType = component.GetType();
         
         // Check if this component is one of our expected enum types
         if(!componentEnumTypes.Contains(componentType))
         {
            var expectedTypes = string.Join(", ", componentEnumTypes.Select(t => t.Name));
            throw new ArgumentException(
               $"Component at index {i} must be of type {expectedTypes}, " +
               $"but was {componentType.Name}");
         }

         // Use reflection to call ComponentSkipSpecification.Skip<T>(component, reason)
         var skipMethod = typeof(ComponentSkipSpecification)
            .GetMethod(nameof(ComponentSkipSpecification.Skip), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(componentType);

         var skipSpec = (string)skipMethod.Invoke(null, [component, skipReasons[i]])!;
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
      return PluggableComponentsReader
            .Permutations
            .Select(ITheoryDataRow (permutation) =>
             {
                var exclusion = SkippedComponents.FindMatchingExclusion(permutation);
                return new TheoryDataRow(permutation.ToString())
                       {
                          Skip = exclusion != null ? $"{exclusion.ComponentName}: {exclusion.Reason}" : null
                       };
             })
            .ToArray();
   }

   public bool SupportsDiscoveryEnumeration() => true; // Yes, we can enumerate at discovery time
}

/// <summary>
/// Alias for PluggableComponentsTheoryAttribute
/// Pluggable Components Theory Attribute
/// Use this attribute instead of [XFact] for tests that should run with all pluggable component combinations.
/// Automatically discovers combinations and injects a PluggableComponentTestContext into TestEnv.
/// Use TestEnv to access the component and the information.
/// </summary>
public sealed class PCTAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
   : PluggableComponentsTheoryAttribute(sourceFilePath, sourceLineNumber) {}
#pragma warning restore CA1813 //avoid unsealed attributes
