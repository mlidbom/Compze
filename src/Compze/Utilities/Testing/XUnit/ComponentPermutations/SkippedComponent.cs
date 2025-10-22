namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

/// <summary>Represents a skipped component with a reason.</summary>
internal class SkippedComponent
{
   public Enum Component { get; }
   public string Reason { get; }

   public SkippedComponent(Enum component, string reason)
   {
      Component = component ?? throw new ArgumentNullException(nameof(component));
      Reason = reason ?? throw new ArgumentNullException(nameof(reason));

      if(string.IsNullOrWhiteSpace(reason))
         throw new ArgumentException("Reason cannot be empty", nameof(reason));
   }

   public bool Skips(ComponentsPermutation permutation) =>
      permutation.Components.Any(c => c.Equals(Component));

   public override string ToString() => $"{Component}: {Reason}";
}
