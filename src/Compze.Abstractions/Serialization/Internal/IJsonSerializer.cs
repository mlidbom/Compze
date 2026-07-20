namespace Compze.Abstractions.Serialization.Internal;

interface IJsonSerializer
{
   string Serialize(object instance);
   object Deserialize(Type type, string json);
   TObject Deserialize<TObject>(string json) => (TObject)Deserialize(typeof(TObject), json);
}
