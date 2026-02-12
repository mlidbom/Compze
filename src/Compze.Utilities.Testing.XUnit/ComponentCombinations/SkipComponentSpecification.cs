using System;
using System.Linq;

namespace Compze.Utilities.Testing.XUnit.ComponentCombinations;

/// <summary>Represents a skipped component with a reason.</summary>
internal class SkipComponentSpecification
{
   readonly Enum _component;
   readonly string _reason;

   public SkipComponentSpecification(Enum component, string reason)
   {
      _component = component ?? throw new ArgumentNullException(nameof(component));
      _reason = reason ?? throw new ArgumentNullException(nameof(reason));

      if(string.IsNullOrWhiteSpace(reason))
         throw new ArgumentException("Reason cannot be empty", nameof(reason));
   }

   public bool Skips(ComponentCombination combination) =>
      combination.Components.Any(c => c.Equals(_component));

   public override string ToString() => $"{_component}: {_reason}";
}
