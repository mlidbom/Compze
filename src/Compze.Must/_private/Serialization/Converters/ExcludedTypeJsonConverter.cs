using Compze.Internals.SystemCE.ReflectionCE;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>
/// The type rule of <see cref="ISerializationConfig{TValue}.ExcludeType"/> and <see cref="MustSerialization.ExcludeType"/>:
/// writes every value of the excluded type, or of a type derived from it, as a short marker naming the value's type,
/// instead of serializing it.
/// </summary>
/// <remarks>
/// An open generic type, such as <c>typeof(List&lt;&gt;)</c>, excludes every type constructed from it and every type derived from one.
/// </remarks>
class ExcludedTypeJsonConverter(Type excluded) : WriteOnlyJsonConverter
{
   readonly Type _excluded = excluded;

   public override bool CanConvert(Type objectType) =>
      _excluded.IsAssignableFrom(objectType) || _excluded.IsGenericTypeDefinition && IsConstructedFromTheExcludedDefinition(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
      writer.WriteValue($"(excluded {value!.GetType().GetFullNameCompilable()})");

   bool IsConstructedFromTheExcludedDefinition(Type objectType) =>
      _excluded.IsInterface
         ? objectType.ImplementsGenericInterface(_excluded)
         : objectType.ClassInheritanceChain().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == _excluded);
}
