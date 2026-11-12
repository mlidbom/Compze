using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>
/// The types Must writes as a short description instead of serializing their members, because serializing them can
/// never work: exceptions, delegates, reflection objects and tasks. Each converter's remarks say what went wrong.
/// </summary>
static class DescribedTypes
{
   static readonly IReadOnlyList<JsonConverter> Converters =
   [
      new ExceptionJsonConverter(),
      new DelegateDescriptionJsonConverter(),
      new ReflectionObjectDescriptionJsonConverter(),
      new TaskDescriptionJsonConverter()
   ];

   public static JsonConverter? ConverterFor(Type type) => Converters.FirstOrDefault(it => it.CanConvert(type));
}
