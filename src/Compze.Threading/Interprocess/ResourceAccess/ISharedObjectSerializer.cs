namespace Compze.Threading.Interprocess.ResourceAccess;

///<summary>Serializes and deserializes instances of <typeparamref name="T"/> to and from byte arrays for file-backed cross-process sharing.</summary>
public interface ISharedObjectSerializer<T> where T : class
{
   ///<summary>Serializes <paramref name="instance"/> to a byte array.</summary>
   byte[] Serialize(T instance);
   ///<summary>Deserializes a <typeparamref name="T"/> instance from <paramref name="data"/>.</summary>
   T Deserialize(byte[] data);
}
