using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   public static ComponentsPermutation? Current => CurrentInternal.Value?.Value;

   public readonly IReadOnlyList<Enum> Components;

   public override string ToString() => string.Join(Separator, Components.Select(it => it.ToString()));

   internal static ComponentsPermutation Parse(string value, Type[] componentEnumTypes) =>
      FromComponentNamesArray(value.Split(Separator), componentEnumTypes);

   internal const string Separator = ":";

   ComponentsPermutation(IReadOnlyList<Enum> components) => Components = components;

   internal static ComponentsPermutation FromComponentNamesArray(string[] componentNames, Type[] componentEnumTypes)
   {
      if(componentNames.Length != componentEnumTypes.Length)
         throw new ArgumentException($"Component name count ({componentNames.Length}) does not match type count ({componentEnumTypes.Length})");

      return new ComponentsPermutation(componentNames.Zip(componentEnumTypes, NameToEnum).ToList());
   }

   static Enum NameToEnum(string componentName, Type enumType)
   {
      try
      {
         return (Enum)Enum.Parse(enumType, componentName);
      }
      catch(ArgumentException ex)
      {
         throw new ArgumentException($"Invalid component value '{componentName}' for enum type {enumType}", ex);
      }
   }

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
