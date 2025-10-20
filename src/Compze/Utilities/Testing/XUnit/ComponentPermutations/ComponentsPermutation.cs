namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, Components);

   internal readonly IReadOnlyList<string> Components;
   ComponentsPermutation(IReadOnlyList<string> components) => Components = components;

   internal static ComponentsPermutation FromArray(string[] value) =>
      new(value);

   internal static ComponentsPermutation Parse(string value) =>
      new(value.Split(Separator));
}
