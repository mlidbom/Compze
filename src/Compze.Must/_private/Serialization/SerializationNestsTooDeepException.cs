namespace Compze.Must._private.Serialization;

/// <summary>
/// Thrown by <see cref="NestingLimitingJsonWriter"/> when an object graph nests deeper than Must serializes: deeper than
/// <see cref="NestingLimitingJsonWriter.DeepestNesting"/> levels, or than the thread's stack allows.
/// </summary>
class SerializationNestsTooDeepException(int depth)
   : Exception($"The object graph nests deeper than Must serializes: Must stopped {depth} levels down. It serializes at most {NestingLimitingJsonWriter.DeepestNesting} levels, and fewer when the thread's stack runs out first.");
