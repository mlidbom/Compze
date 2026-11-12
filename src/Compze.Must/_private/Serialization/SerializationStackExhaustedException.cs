namespace Compze.Must._private.Serialization;

/// <summary>Thrown by <see cref="StackGuardingJsonWriter"/> when an object graph nests deeper than the thread's stack allows Must to serialize.</summary>
class SerializationStackExhaustedException(int depth) : Exception($"The object graph nests deeper than this thread's stack allows: Must stopped {depth} levels down.");
