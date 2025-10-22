namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>Represents an exclusion of a component with a reason.</summary>
internal class ComponentExclusion
{
   public Enum Component { get; }
   public string Reason { get; }

   public ComponentExclusion(Enum component, string reason)
   {
      Component = component ?? throw new ArgumentNullException(nameof(component));
      Reason = reason ?? throw new ArgumentNullException(nameof(reason));

      if(string.IsNullOrWhiteSpace(reason))
         throw new ArgumentException("Reason cannot be empty", nameof(reason));
   }

   public bool Excludes(ComponentsPermutation permutation) =>
      permutation.Components.Any(c => c.Equals(Component));
}
