namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, Components);

   public readonly IReadOnlyList<string> Components;
   ComponentsPermutation(IReadOnlyList<string> components) => Components = components;

   internal static ComponentsPermutation FromArray(string[] value) =>
      new(value);

   internal static ComponentsPermutation Parse(string value) =>
      new(value.Split(Separator));

   public static ComponentsPermutation? Current => CurrentInternal.Value;
   static readonly AsyncLocal<ComponentsPermutation?> CurrentInternal = new();

   internal static async Task<TReturn> RunInContextAsync<TReturn>(
      ComponentsPermutation permutation,
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
