using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, ComponentStrings);

   /// <summary>
   /// Components as Enum values when types are provided, otherwise as strings.
   /// Cast to Enum or string as appropriate based on your context.
   /// </summary>
   public readonly IReadOnlyList<object> Components;
   
   /// <summary>
   /// Components as strings for display and matching purposes.
   /// </summary>
   public readonly IReadOnlyList<string> ComponentStrings;

   /// <summary>
   /// Components cast as Enum values. Use this when you know the components are typed.
   /// Will throw if components are strings (when using untyped PCT).
   /// </summary>
   public IReadOnlyList<Enum> ComponentEnums => Components.Cast<Enum>().ToList();
   
   ComponentsPermutation(IReadOnlyList<object> components, IReadOnlyList<string> componentStrings)
   {
      Components = components;
      ComponentStrings = componentStrings;
   }

   internal static ComponentsPermutation FromArray(string[] value, Type[]? componentEnumTypes = null)
   {
      if(componentEnumTypes == null || componentEnumTypes.Length == 0)
      {
         // No type information - store as strings (for backward compatibility with untyped PCT)
         return new(value, value);
      }

      if(value.Length != componentEnumTypes.Length)
         throw new ArgumentException($"Component count ({value.Length}) does not match type count ({componentEnumTypes.Length})");

      var componentEnums = new Enum[value.Length];
      for(int i = 0; i < value.Length; i++)
      {
         if(!componentEnumTypes[i].IsEnum)
            throw new ArgumentException($"Type {componentEnumTypes[i].Name} must be an enum type");

         try
         {
            componentEnums[i] = (Enum)Enum.Parse(componentEnumTypes[i], value[i]);
         }
         catch(ArgumentException ex)
         {
            throw new ArgumentException($"Invalid component value '{value[i]}' for type {componentEnumTypes[i].Name}", ex);
         }
      }

      // Cast to object[] for storage since Components must hold both strings (untyped) and Enums (typed)
      return new(componentEnums.Cast<object>().ToArray(), value);
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
