namespace Compze.Core.Serialization.Internal.DbPool;

interface ISharedObjectSerializer<TShared>
   where TShared : class
{
   string Serialize(TShared instance);
   TShared Deserialize(string json);
}
