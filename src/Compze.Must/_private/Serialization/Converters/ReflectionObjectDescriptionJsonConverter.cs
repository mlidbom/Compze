using System.Reflection;
using Compze.Internals.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>
/// Writes a reflection object, a method, property, field, event, constructor, parameter, assembly or module,
/// as a short description naming it, instead of serializing it.
/// </summary>
/// <remarks>
/// A reflection object's members are the runtime's internals: their private getters assume preconditions nothing
/// outside the runtime honors, and one killed the test process with a fatal CLR error. Read only through its public
/// getters, a reflection object leads on to its assembly and every type in it, and serializing that ran out of memory.
/// A <see cref="Type"/> is not one of these: Newtonsoft writes it as its assembly-qualified name.
/// </remarks>
class ReflectionObjectDescriptionJsonConverter : WriteOnlyJsonConverter
{
   public override bool CanConvert(Type objectType) =>
      typeof(MemberInfo).IsAssignableFrom(objectType) && !typeof(Type).IsAssignableFrom(objectType)
      || typeof(ParameterInfo).IsAssignableFrom(objectType)
      || typeof(Assembly).IsAssignableFrom(objectType)
      || typeof(Module).IsAssignableFrom(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
      writer.WriteValue(value switch
      {
         MemberInfo member       => $"({member.MemberType} {NameOf(member)})",
         ParameterInfo parameter => $"(parameter {parameter.Name} of {NameOf(parameter.Member)})",
         Assembly assembly       => $"(assembly {assembly.FullName})",
         Module module           => $"(module {module.Name})",
         _                       => throw new InvalidOperationException($"{value!.GetType()} is not a reflection object")
      });

   static string NameOf(MemberInfo member) => $"{member.DeclaringType?.GetFullNameCompilable()}.{member.Name}";
}
