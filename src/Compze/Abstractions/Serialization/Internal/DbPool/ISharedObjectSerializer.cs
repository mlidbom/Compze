namespace Compze.Core.Serialization.Internal.DbPool;

interface ISharedObjectSerializer
{
   string Serialize(object instance);
   TShared Deserialize<TShared>(string json) where TShared : class;
}
