using Newtonsoft.Json.Serialization;

namespace Compze.Must._private.Serialization;

/// <summary>
/// Names types for the <c>$type</c> annotations Must writes, exactly as Newtonsoft's default binder does, and refuses to
/// resolve a type from JSON: Must never reads JSON, so the type names it writes can never be used to create an arbitrary type.
/// </summary>
class WriteOnlySerializationBinder : DefaultSerializationBinder
{
   public override Type BindToType(string? assemblyName, string typeName) =>
      throw new NotSupportedException("Must only writes JSON; it never reads any.");
}
