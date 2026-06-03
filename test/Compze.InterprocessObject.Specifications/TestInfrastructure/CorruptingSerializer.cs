namespace Compze.InterprocessObject.Specifications.TestInfrastructure;

class CorruptingSerializer : IInterprocessObjectSerializer<SharedObject>
{
   readonly SharedObjectSerializer _inner = new();
   public bool FailOnDeserialize { get; set; }

   public byte[] Serialize(SharedObject instance) => _inner.Serialize(instance);

   public SharedObject Deserialize(byte[] data) =>
      FailOnDeserialize ? throw new InvalidOperationException("Simulated deserialization corruption") : _inner.Deserialize(data);
}
