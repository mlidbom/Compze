using Compze.Internals.SystemCE;
using System.ComponentModel;
using Compze.Internals.SystemCE.Core.ThreadingCE.TasksCE;
using Xunit.Sdk;

namespace Compze.xUnitMatrix;

public class ComponentCombination : IXunitSerializable
{
#pragma warning disable CA1065 //throwing in a property.
   public static ComponentCombination Current => TryGetCurrent() ?? throw new Exception("Found no current combination");
#pragma warning restore CA1065

   public static ComponentCombination? TryGetCurrent() => CurrentInternal.Value?.Value;

   public IReadOnlyList<Enum> Components { get; private set; }

   public override string ToString() => string.Join(Separator, Components.Select(it => it.ToString()));

   internal const string Separator = ":";

   [Obsolete("Called by xUnit deserializer", error: true)]
   // ReSharper disable once UnusedMember.Global
   public ComponentCombination() => Components = [];

   ComponentCombination(IEnumerable<Enum> components) => Components = components.ToList();

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

      var combination = FromComponentNamesList(componentNames, componentTypes);
      Components = combination.Components;
   }

   internal static ComponentCombination FromComponentEnumValues(IEnumerable<Enum> componentEnumValues) => new(componentEnumValues);

   static ComponentCombination FromComponentNamesList(IReadOnlyList<string> componentNames, Type[] componentEnumTypes)
   {
      if(componentNames.Count != componentEnumTypes.Length)
         throw new ArgumentException($"Components: [{string.Join(", ", componentNames)}] do not match specified component types [{string.Join(", ", componentEnumTypes.Select(it => it.Name))}]");

      return new ComponentCombination(componentNames.Zip(componentEnumTypes, NameToEnum).ToList());
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

   static readonly AsyncLocal<LazyCE<ComponentCombination>?> CurrentInternal = new();

   internal static async Task<TReturn> RunInContextAsync<TReturn>(
      LazyCE<ComponentCombination> combination,
      Func<Task<TReturn>> executeTest)
   {
      CurrentInternal.Value = combination;
      try
      {
         return await executeTest().caf();
      }
      finally
      {
         CurrentInternal.Value = null;
      }
   }
}
