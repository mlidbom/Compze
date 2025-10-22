using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, Components.Select(it => it.ToString()));

   /// <summary>Components as enum values. Always strongly typed.</summary>
   public readonly IReadOnlyList<Enum> Components;

   ComponentsPermutation(IReadOnlyList<Enum> components) => Components = components;

   /// <summary>
   /// Creates a ComponentsPermutation from string array, parsing components as enums.
   /// </summary>
   /// <param name="componentNames">Component names as strings from the file</param>
   /// <param name="componentEnumTypes">Enum types for each component position.
   /// </param>
   internal static ComponentsPermutation FromComponentNamesArray(string[] componentNames, Type[] componentEnumTypes)
   {
      if(componentNames.Length != componentEnumTypes.Length)
         throw new ArgumentException($"Component count ({componentNames.Length}) does not match type count ({componentEnumTypes.Length})");

      for(int i = 0; i < componentNames.Length; i++)
      {
         if(!componentEnumTypes[i].IsEnum)
            throw new ArgumentException($"Type {componentEnumTypes[i].Name} must be an enum type");
      }

      return new(componentNames.Zip(componentEnumTypes, NameToEnum).ToList());
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

   internal static ComponentsPermutation Parse(string value, Type[] componentEnumTypes) =>
      FromComponentNamesArray(value.Split(Separator), componentEnumTypes);

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
