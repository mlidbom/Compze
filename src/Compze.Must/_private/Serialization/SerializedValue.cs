using System.Globalization;
using System.Text;
using Compze.Contracts;
using Compze.Must._private.Serialization.Rules;
using Newtonsoft.Json;

namespace Compze.Must._private.Serialization;

/// <summary>
/// A value serialized the way Must shows and compares values: its JSON, or, when Must could not serialize it, the
/// <see cref="SerializationFailure"/> that says why.
/// </summary>
/// <remarks>
/// Serializing a value runs code Must does not control, every getter in the object graph, so it can fail. A failure
/// must never take the assertion's own message with it, and must never take the test process down: the
/// <see cref="FirstSerializationError"/> handles every error where it is thrown, and the
/// <see cref="StackGuardingJsonWriter"/> stops a graph nested too deep for the stack.
/// </remarks>
class SerializedValue
{
   SerializedValue(string? json, SerializationFailure? failure)
   {
      Json = json;
      Failure = failure;
   }

   /// <summary>Why Must could not serialize the value, or null when it could.</summary>
   public SerializationFailure? Failure { get; }

   public bool Succeeded => Failure == null;

   /// <summary>The value's JSON. Only a value that <see cref="Succeeded"/> has one.</summary>
   public string Json => field._assert().NotNull();

   public static SerializedValue Of(object? value, MemberVisibility visibility, SerializationRules rules)
   {
      var serializer = JsonSerializer.Create(rules.SettingsFor(visibility));
      var firstError = new FirstSerializationError(serializer);

      using var json = new StringWriter(new StringBuilder(256), CultureInfo.InvariantCulture);
      using(var writer = new StackGuardingJsonWriter(json))
      {
         writer.Formatting = serializer.Formatting;
         serializer.Serialize(writer, value);
      }

      return firstError.Failure is {} failure
                ? new SerializedValue(null, failure)
                : new SerializedValue(json.ToString(), null);
   }
}
