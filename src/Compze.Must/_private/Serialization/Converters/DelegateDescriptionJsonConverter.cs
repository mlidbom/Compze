using Compze.Internals.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>Writes a delegate as a short description, its type and the methods it calls, instead of serializing it.</summary>
/// <remarks>
/// Serializing a delegate's members walks from its method into the runtime's reflection objects, whose private getters
/// assume preconditions nothing outside the runtime honors. Calling them killed the test process with a fatal CLR
/// error, <c>0x80131506</c>, which no catch can stop.
/// </remarks>
class DelegateDescriptionJsonConverter : WriteOnlyJsonConverter
{
   public override bool CanConvert(Type objectType) => typeof(Delegate).IsAssignableFrom(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
   {
      var @delegate = (Delegate)value!;
      var calledMethods = @delegate.GetInvocationList()
                                   .Select(it => $"{it.Method.DeclaringType?.GetFullNameCompilable()}.{it.Method.Name}");
      writer.WriteValue($"(delegate {@delegate.GetType().GetFullNameCompilable()} calling {string.Join(", ", calledMethods)})");
   }
}
