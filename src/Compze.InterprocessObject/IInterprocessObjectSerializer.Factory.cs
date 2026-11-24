using System.Text.Json;
using Compze.InterprocessObject._private;

namespace Compze.InterprocessObject;

///<summary>Factory for the <see cref="IInterprocessObjectSerializer{T}"/>s that come with Compze.InterprocessObject.</summary>
public interface IInterprocessObjectSerializer
{
   ///<summary>
   /// Keeps the object as UTF-8 JSON, with System.Text.Json's <see cref="JsonSerializer"/> and <paramref name="options"/>, or
   /// its defaults when they are null: for an object that needs no attributes or source generator, as MemoryPack's does,
   /// and whose file a human can read.
   ///</summary>
   ///<remarks>
   /// JSON is larger and slower to write and read than MemoryPack's binary form, which Compze.InterprocessObject.MemoryPack
   /// offers. Every reader and writer of an object must use the same options.
   ///</remarks>
   static IInterprocessObjectSerializer<T> Json<T>(JsonSerializerOptions? options = null) where T : class => new JsonInterprocessObjectSerializer<T>(options);
}
