namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

interface ISharedObjectSerializer
{
   string Serialize(object instance);
   TShared Deserialize<TShared>(string json) where TShared : class;
}
