using System.Runtime.CompilerServices;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Type-safe version of PluggableComponentsTheoryAttribute with two component dimensions.
/// Instead of using string arrays for skipped components, use strongly-typed enum values in constructor.
/// Due to C# attribute limitations, we use object arrays that must contain enum values.
/// </summary>
/// <typeparam name="TComponent1">First component dimension enum type</typeparam>
/// <typeparam name="TComponent2">Second component dimension enum type</typeparam>
public abstract class TypedPluggableComponentsTheoryAttribute<TComponent1, TComponent2> : PluggableComponentsTheoryAttribute
   where TComponent1 : Enum
   where TComponent2 : Enum
{
   /// <summary>
   /// Creates a type-safe pluggable components theory attribute.
   /// </summary>
   /// <param name="skippedComponents1">Array of TComponent1 enum values to skip</param>
   /// <param name="skipReasons1">Corresponding reasons for skipping (must match length of skippedComponents1)</param>
   /// <param name="skippedComponents2">Array of TComponent2 enum values to skip</param>
   /// <param name="skipReasons2">Corresponding reasons for skipping (must match length of skippedComponents2)</param>
   protected TypedPluggableComponentsTheoryAttribute(
      object[]? skippedComponents1 = null,
      string[]? skipReasons1 = null,
      object[]? skippedComponents2 = null,
      string[]? skipReasons2 = null,
      [CallerFilePath] string? sourceFilePath = null,
      [CallerLineNumber] int sourceLineNumber = -1)
      : base(sourceFilePath, sourceLineNumber)
   {
      var skipped = new List<string>();

      if(skippedComponents1 != null && skipReasons1 != null)
      {
         if(skippedComponents1.Length != skipReasons1.Length)
            throw new ArgumentException("Number of components must match number of reasons for dimension 1");

         for(int i = 0; i < skippedComponents1.Length; i++)
         {
            if(skippedComponents1[i] is not TComponent1 component)
               throw new ArgumentException($"Component at index {i} in dimension 1 must be of type {typeof(TComponent1).Name}");

            skipped.Add(ComponentSkipSpecification.Skip(component, skipReasons1[i]));
         }
      }

      if(skippedComponents2 != null && skipReasons2 != null)
      {
         if(skippedComponents2.Length != skipReasons2.Length)
            throw new ArgumentException("Number of components must match number of reasons for dimension 2");

         for(int i = 0; i < skippedComponents2.Length; i++)
         {
            if(skippedComponents2[i] is not TComponent2 component)
               throw new ArgumentException($"Component at index {i} in dimension 2 must be of type {typeof(TComponent2).Name}");

            skipped.Add(ComponentSkipSpecification.Skip(component, skipReasons2[i]));
         }
      }

      Skipped = [..skipped];
   }
}
