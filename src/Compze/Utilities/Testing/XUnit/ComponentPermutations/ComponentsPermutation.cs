using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, ComponentStrings);

   /// <summary>Components as enum values. Always strongly typed.</summary>
   public readonly IReadOnlyList<Enum> Components;
   
   /// <summary>
   /// Components as string values for display and matching.
   /// </summary>
   public readonly IReadOnlyList<string> ComponentStrings;
   
   ComponentsPermutation(IReadOnlyList<Enum> components, IReadOnlyList<string> componentStrings)
   {
      Components = components;
      ComponentStrings = componentStrings;
   }

   /// <summary>
   /// Creates a ComponentsPermutation from string array, parsing components as enums.
   /// </summary>
   /// <param name="componentStringValues">Component names as strings from the file</param>
   /// <param name="componentEnumTypes">Enum types for each component position, from <see cref="TypedPCTAttribute"/>>.
   /// </param>
   internal static ComponentsPermutation FromArray(string[] componentStringValues, Type[] componentEnumTypes)
   {
      if(componentStringValues.Length != componentEnumTypes.Length)
         throw new ArgumentException($"Component count ({componentStringValues.Length}) does not match type count ({componentEnumTypes.Length})");

      var componentEnums = new Enum[componentStringValues.Length];
      for(int i = 0; i < componentStringValues.Length; i++)
      {
         if(!componentEnumTypes[i].IsEnum)
            throw new ArgumentException($"Type {componentEnumTypes[i].Name} must be an enum type");

         try
         {
            componentEnums[i] = (Enum)Enum.Parse(componentEnumTypes[i], componentStringValues[i]);
         }
         catch(ArgumentException ex)
         {
            throw new ArgumentException($"Invalid component value '{componentStringValues[i]}' for type {componentEnumTypes[i].Name}", ex);
         }
      }

      return new(componentEnums, componentStringValues);
   }

   internal static ComponentsPermutation Parse(string value, Type[] componentEnumTypes) =>
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
