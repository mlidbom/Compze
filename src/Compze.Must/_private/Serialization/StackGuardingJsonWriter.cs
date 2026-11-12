using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization;

/// <summary>
/// A <see cref="JsonTextWriter"/> that refuses to start another object or array when the thread's stack is nearly used up,
/// throwing a <see cref="SerializationStackExhaustedException"/> instead of letting the next level overflow the stack.
/// </summary>
/// <remarks>
/// A stack overflow kills the process and every test running in it, and no catch can stop it. With this writer, a graph
/// nested too deep for the thread's stack fails the way any other unserializable value does. How deep that is depends
/// on the stack: about a thousand levels per megabyte.
/// </remarks>
/// <remarks>
/// The check alone is not enough: Newtonsoft catches and rethrows an exception at every level it unwinds through, at a
/// cost of about 10 KB of stack per level, so an exception thrown deep down would overflow the stack anyway.
/// <see cref="SerializedValue"/> handles every serialization error at the level it is thrown, which avoids that.
/// </remarks>
class StackGuardingJsonWriter(TextWriter textWriter) : JsonTextWriter(textWriter)
{
   public override void WriteStartObject()
   {
      ThrowIfTheStackIsNearlyUsedUp();
      base.WriteStartObject();
   }

   public override void WriteStartArray()
   {
      ThrowIfTheStackIsNearlyUsedUp();
      base.WriteStartArray();
   }

   void ThrowIfTheStackIsNearlyUsedUp()
   {
      if(!RuntimeHelpers.TryEnsureSufficientExecutionStack())
         throw new SerializationStackExhaustedException(Top);
   }
}
