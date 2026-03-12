namespace Compze.InterprocessObject;

///<summary>Serializes and deserializes instances of <typeparamref name="T"/> to and from byte arrays for interprocess storage.</summary>
public interface IInterprocessObjectSerializer<T> where T : class
{
   ///<summary>Serializes <paramref name="instance"/> to a byte array.</summary>
   byte[] Serialize(T instance);

   ///<summary>Deserializes a byte array back to an instance of <typeparamref name="T"/>.</summary>
   T Deserialize(byte[] data);
}
