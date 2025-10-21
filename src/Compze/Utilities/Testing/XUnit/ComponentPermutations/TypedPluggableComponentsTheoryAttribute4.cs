using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Type-safe version of PluggableComponentsTheoryAttribute with four component dimensions.
/// Instead of using string arrays for skipped components, use strongly-typed enum values in constructor.
/// Due to C# attribute limitations, we use object arrays that must contain enum values.
/// </summary>
/// <typeparam name="TComponent1">First component dimension enum type</typeparam>
/// <typeparam name="TComponent2">Second component dimension enum type</typeparam>
/// <typeparam name="TComponent3">Third component dimension enum type</typeparam>
/// <typeparam name="TComponent4">Fourth component dimension enum type</typeparam>
public abstract class TypedPluggableComponentsTheoryAttribute<TComponent1, TComponent2, TComponent3, TComponent4> : PluggableComponentsTheoryAttribute
   where TComponent1 : Enum
   where TComponent2 : Enum
   where TComponent3 : Enum
   where TComponent4 : Enum
{
   /// <summary>
   /// Creates a type-safe pluggable components theory attribute.
   /// </summary>
   /// <param name="skippedComponents">Array of enum values to skip (can be any of TComponent1, TComponent2, TComponent3, or TComponent4)</param>
   /// <param name="skipReasons">Corresponding reasons for skipping (must match length of skippedComponents)</param>
   protected TypedPluggableComponentsTheoryAttribute(
      object[]? skippedComponents = null,
      string[]? skipReasons = null,
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
      : base(sourceFilePath, sourceLineNumber)
   {
      if(skippedComponents == null || skipReasons == null)
      {
         Skipped = [];
         return;
      }

      if(skippedComponents.Length != skipReasons.Length)
         throw new ArgumentException("Number of components must match number of reasons");

      var skipped = new List<string>();
      
      for(int i = 0; i < skippedComponents.Length; i++)
      {
         var component = skippedComponents[i];
         
         // Validate that the component is one of our expected enum types
         if(component is TComponent1 comp1)
         {
            skipped.Add(ComponentSkipSpecification.Skip(comp1, skipReasons[i]));
         }
         else if(component is TComponent2 comp2)
         {
            skipped.Add(ComponentSkipSpecification.Skip(comp2, skipReasons[i]));
         }
         else if(component is TComponent3 comp3)
         {
            skipped.Add(ComponentSkipSpecification.Skip(comp3, skipReasons[i]));
         }
         else if(component is TComponent4 comp4)
         {
            skipped.Add(ComponentSkipSpecification.Skip(comp4, skipReasons[i]));
         }
         else
         {
            throw new ArgumentException(
               $"Component at index {i} must be of type {typeof(TComponent1).Name}, {typeof(TComponent2).Name}, {typeof(TComponent3).Name}, or {typeof(TComponent4).Name}, " +
               $"but was {component?.GetType().Name ?? "null"}");
         }
      }

      Skipped = [..skipped];
   }
}
