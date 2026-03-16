namespace Compze.xUnitMatrix;

/// <summary>Represents a skipped component with a reason.</summary>
class ComponentSkipSpecification
{
   readonly Enum _component;
   readonly string _reason;

   internal ComponentSkipSpecification(Enum component, string reason)
   {
      _component = component ?? throw new ArgumentNullException(nameof(component));
      _reason = reason ?? throw new ArgumentNullException(nameof(reason));

      if(string.IsNullOrWhiteSpace(reason))
         throw new ArgumentException("Reason cannot be empty", nameof(reason));
   }

   internal Type ComponentType => _component.GetType();
   internal Enum ComponentValue => _component;

   internal bool Skips(MatrixCombination combination) =>
      combination.Components.Any(c => c.Equals(_component));

   public override string ToString() => $"{_component}: {_reason}";
}
