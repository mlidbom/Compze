using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, ComponentStrings);

   public readonly IReadOnlyList<object> Components;
   public readonly IReadOnlyList<string> ComponentStrings;
   
   ComponentsPermutation(IReadOnlyList<object> components, IReadOnlyList<string> componentStrings)
   {
      Components = components;
      ComponentStrings = componentStrings;
   }

   internal static ComponentsPermutation FromArray(string[] value, Type[]? componentEnumTypes = null)
   {
      if(componentEnumTypes == null || componentEnumTypes.Length == 0)
      {
         // No type information - store as strings
         return new(value, value);
      }

      if(value.Length != componentEnumTypes.Length)
         throw new ArgumentException($"Component count ({value.Length}) does not match type count ({componentEnumTypes.Length})");

      var components = new object[value.Length];
      for(int i = 0; i < value.Length; i++)
      {
         if(!componentEnumTypes[i].IsEnum)
            throw new ArgumentException($"Type {componentEnumTypes[i].Name} must be an enum type");

         try
         {
            components[i] = Enum.Parse(componentEnumTypes[i], value[i]);
         }
         catch(ArgumentException ex)
         {
            throw new ArgumentException($"Invalid component value '{value[i]}' for type {componentEnumTypes[i].Name}", ex);
         }
      }

      return new(components, value);
   }

   internal static ComponentsPermutation Parse(string value, Type[]? componentEnumTypes = null) =>
      FromArray(value.Split(Separator), componentEnumTypes);

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
