using Compze.Utilities.SystemCE;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation
{
   internal const string Separator = ":";

   public override string ToString() => string.Join(Separator, ComponentStrings);

   /// <summary>
   /// Components as enum values (when using TypedPCT) or strings (when using legacy PCT).
   /// Use ComponentEnums property to access as enums when you know they're typed.
   /// </summary>
   public readonly IReadOnlyList<object> Components;
   
   /// <summary>
   /// Components as string values, always available for display and matching.
   /// </summary>
   public readonly IReadOnlyList<string> ComponentStrings;

   /// <summary>
   /// Components cast to enum values. Only use this when you know the components are typed (TypedPCT).
   /// Will throw InvalidCastException if components are strings (legacy PCT).
   /// </summary>
   public IReadOnlyList<Enum> ComponentEnums => Components.Cast<Enum>().ToList();
   
   ComponentsPermutation(IReadOnlyList<object> components, IReadOnlyList<string> componentStrings)
   {
      Components = components;
      ComponentStrings = componentStrings;
   }

   /// <summary>
   /// Creates a ComponentsPermutation from string array.
   /// </summary>
   /// <param name="value">Component names as strings</param>
   /// <param name="componentEnumTypes">
   /// Enum types for each component position. When provided, components are parsed as enums.
   /// When null, components remain as strings (for backward compatibility with legacy PCT).
   /// The attribute's type information flows through: TypedPCTAttribute → Reader → FromArray.
   /// </param>
   internal static ComponentsPermutation FromArray(string[] value, Type[]? componentEnumTypes = null)
   {
      if(componentEnumTypes == null || componentEnumTypes.Length == 0)
      {
         // No type information - store as strings (legacy untyped PCT)
         return new(value, value);
      }

      // Type information provided - parse as enums
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

      // Store as object array (must support both strings for legacy and enums for typed)
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
