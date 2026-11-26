using System.Text.Json;

namespace Compze.Threading.Interprocess.ResourceAccess._private;

///<summary>An <see cref="IInterprocessObjectSerializer{T}"/> that keeps the object as UTF-8 JSON, with System.Text.Json's <see cref="JsonSerializer"/>.</summary>
sealed class JsonInterprocessObjectSerializer<T> : IInterprocessObjectSerializer<T> where T : class
{
   readonly JsonSerializerOptions? _options;

   internal JsonInterprocessObjectSerializer(JsonSerializerOptions? options) => _options = options;

   public byte[] Serialize(T instance) => JsonSerializer.SerializeToUtf8Bytes(instance, _options);

   public T Deserialize(byte[] data) =>
      JsonSerializer.Deserialize<T>(data, _options) ?? throw new JsonException($"The interprocess object's JSON is null, not a {typeof(T).FullName}.");
}
