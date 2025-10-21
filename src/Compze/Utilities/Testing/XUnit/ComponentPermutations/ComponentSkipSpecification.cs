namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>
/// Helper class to create type-safe component skip specifications.
/// Converts enum values to the "ComponentName::Reason" format used by the exclusion system.
/// </summary>
public static class ComponentSkipSpecification
{
   /// <summary>
   /// Creates a skip specification from an enum value and reason.
   /// Format: "EnumValueName::Reason"
   /// </summary>
   /// <typeparam name="TComponent">The enum type representing the component dimension</typeparam>
   /// <param name="component">The specific component to skip</param>
   /// <param name="reason">The reason for skipping (mandatory)</param>
   /// <returns>A formatted skip specification string</returns>
   public static string Skip<TComponent>(TComponent component, string reason)
      where TComponent : Enum
   {
      if(string.IsNullOrWhiteSpace(reason))
         throw new ArgumentException("Reason cannot be empty", nameof(reason));

      return $"{component}::{reason}";
   }

   /// <summary>
   /// Creates multiple skip specifications from enum values with reasons.
   /// </summary>
   public static string[] Skip<TComponent>(params (TComponent component, string reason)[] specifications)
      where TComponent : Enum
   {
      return specifications.Select(spec => Skip(spec.component, spec.reason)).ToArray();
   }
}
