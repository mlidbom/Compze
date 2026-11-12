using Newtonsoft.Json;

namespace Compze.Must._private.Serialization.Converters;

/// <summary>
/// The type rule of <see cref="ISerializationConfig{TValue}.SerializeAs{TReplaced}"/> and <see cref="MustSerialization.SerializeAs{TReplaced}"/>:
/// serializes, in place of every value of <typeparamref name="TReplaced"/>, the value the replacement returns for it.
/// </summary>
class SerializeAsJsonConverter<TReplaced>(Func<TReplaced, object?> replacement) : WriteOnlyJsonConverter
{
   readonly Func<TReplaced, object?> _replacement = replacement;

   public override bool CanConvert(Type objectType) => typeof(TReplaced).IsAssignableFrom(objectType);

   public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
      serializer.Serialize(writer, _replacement((TReplaced)value!));
}
