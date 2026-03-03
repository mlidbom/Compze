namespace Compze.Core.Serialization.Internal;

public interface IJsonSerializer
{
   //todo: we should be using the interface, not concrete implementations, so this should not be unused.
   // We should also inject the serializer, not instantiate it manually.
   string Serialize(object instance);
   object Deserialize(Type type, string json);
   TObject Deserialize<TObject>(string json) => (TObject)Deserialize(typeof(TObject), json);
}
