namespace Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

public interface ISharedObjectSerializer<T> where T : class
{
   string Serialize(T instance);
   T Deserialize(string json);
}
