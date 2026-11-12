using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization;

/// <summary>
/// A <see cref="JsonTextWriter"/> that refuses to nest objects and arrays deeper than <see cref="DeepestNesting"/> levels, or deeper
/// than the thread's stack allows, throwing a <see cref="SerializationNestsTooDeepException"/> instead.
/// </summary>
/// <remarks>
/// Each limit keeps a deep graph from taking the test process down. The stack: a stack overflow kills the process and every test
/// running in it, and no catch can stop it. The fixed depth: indented JSON grows with the square of the depth, since every line is
/// indented by its depth, and the stack's limit grows with the stack, about a thousand levels per megabyte. Stopping only where
/// the stack runs out let a thread with a large stack build gigabytes of JSON: the Linux CI runners' test threads ran out of
/// memory, and on a 32 MB stack the out-of-memory exception, thrown that close to the stack's end, overflowed it. Stopping at
/// <see cref="DeepestNesting"/> levels keeps the JSON to a dozen megabytes or so, and makes where Must stops the same on every
/// machine whose test threads have a stack of a megabyte or more.
/// </remarks>
/// <remarks>
/// The checks alone are not enough: Newtonsoft catches and rethrows an exception at every level it unwinds through, at a
/// cost of about 10 KB of stack per level, so an exception thrown deep down would overflow the stack anyway.
/// <see cref="SerializedValue"/> handles every serialization error at the level it is thrown, which avoids that.
/// </remarks>
class NestingLimitingJsonWriter(TextWriter textWriter) : JsonTextWriter(textWriter)
{
   /// <summary>The deepest nesting of objects and arrays Must serializes.</summary>
   internal const int DeepestNesting = 1_000;

   public override void WriteStartObject()
   {
      ThrowIfAnotherLevelWouldNestTooDeep();
      base.WriteStartObject();
   }

   public override void WriteStartArray()
   {
      ThrowIfAnotherLevelWouldNestTooDeep();
      base.WriteStartArray();
   }

   void ThrowIfAnotherLevelWouldNestTooDeep()
   {
      if(AnotherLevelWouldNestTooDeep)
         throw new SerializationNestsTooDeepException(Top);
   }

   bool AnotherLevelWouldNestTooDeep => Top >= DeepestNesting || !RuntimeHelpers.TryEnsureSufficientExecutionStack();
}
