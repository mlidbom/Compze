namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>Serializes and deserializes instances of <typeparamref name="T"/> to and from strings for file-backed cross-process sharing.</summary>
public interface ISharedObjectSerializer<T> where T : class
{
   ///<summary>Serializes <paramref name="instance"/> to a string.</summary>
   string Serialize(T instance);
   ///<summary>Deserializes a string produced by <see cref="Serialize"/> back into a <typeparamref name="T"/> instance.</summary>
   T Deserialize(string json);
}
