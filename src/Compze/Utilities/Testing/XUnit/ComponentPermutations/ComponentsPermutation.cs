using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, Components);

   public readonly IReadOnlyList<string> Components;
   ComponentsPermutation(IReadOnlyList<string> components) => Components = components;

   internal (bool IsExcluded, string? Reason) IsExcludedBy(string[] exclusionSpecs)
   {
      foreach(var exclusionSpec in exclusionSpecs)
      {
         var exclusion = ComponentExclusion.Parse(exclusionSpec);
         if(exclusion.Matches(this))
            return (true, exclusion.Reason);
      }

      return (false, null);
   }

   internal static ComponentsPermutation FromArray(string[] value) =>
      new(value);

   internal static ComponentsPermutation Parse(string value) =>
      new(value.Split(Separator));

   public static ComponentsPermutation? Current => CurrentInternal.Value?.Value;
   static readonly AsyncLocal<LazyCE<ComponentsPermutation>?> CurrentInternal = new();

   internal static async Task<TReturn> RunInContextAsync<TReturn>(
      LazyCE<ComponentsPermutation> permutation,
      Func<Task<TReturn>> executeTest)
   {
      CurrentInternal.Value = permutation;
      try
      {
         return await executeTest();
      }
      finally
      {
         CurrentInternal.Value = null;
      }
   }
}
