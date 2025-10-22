using System.ComponentModel;
using Compze.Utilities.SystemCE;
using Xunit.Sdk;

namespace Compze.Utilities.Testing.XUnit.ComponentPermutations;

public class ComponentsPermutation : IXunitSerializable
{
   public static ComponentsPermutation Current => TryGetCurrent() ?? throw new Exception("Found no current permutation");

   public static ComponentsPermutation? TryGetCurrent() => CurrentInternal.Value?.Value;

   public IReadOnlyList<Enum> Components { get; private set; }

   public override string ToString() => string.Join(Separator, Components.Select(it => it.ToString()));

   internal const string Separator = ":";

   [Obsolete("Called by xUnit deserializer", error: true)]
   public ComponentsPermutation() => Components = [];

   ComponentsPermutation(IEnumerable<Enum> components) => Components = components.ToList();

   public void Serialize(IXunitSerializationInfo info)
   {
      info.AddValue("ComponentNames", Components.Select(it => it.ToString()).ToArray());
      info.AddValue("ComponentTypes", Components.Select(it => it.GetType().AssemblyQualifiedName!).ToArray());
   }

   public void Deserialize(IXunitSerializationInfo info)
   {
      var componentNames = info.GetValue<string[]>("ComponentNames") ?? throw new InvalidEnumArgumentException("Components string is null");
      var componentTypes = (info.GetValue<string[]>("ComponentTypes") ?? throw new InvalidEnumArgumentException("ComponentTypes is null"))
                          .Select(it => Type.GetType(it, throwOnError: true)!)
                          .ToArray();

      var permutation = FromComponentNamesList(componentNames, componentTypes);
      Components = permutation.Components;
   }

   internal static ComponentsPermutation FromComponentEnumValues(IEnumerable<Enum> componentEnumValues) => new(componentEnumValues);

   internal static ComponentsPermutation FromComponentNamesList(IReadOnlyList<string> componentNames, Type[] componentEnumTypes)
   {
      if(componentNames.Count != componentEnumTypes.Length)
         throw new ArgumentException($"Components: [{string.Join(", ", componentNames)}] do not match specified component types [{string.Join(", ", componentEnumTypes.Select(it=>it.Name))}]");

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
