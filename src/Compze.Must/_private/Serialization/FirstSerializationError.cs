using Newtonsoft.Json;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Compze.Must._private.Serialization;

/// <summary>
/// Handles every error a <see cref="JsonSerializer"/> reports, at the level of the object graph it is thrown at, and keeps
/// the first as the <see cref="Failure"/>.
/// </summary>
/// <remarks>
/// Newtonsoft catches and rethrows an unhandled error at every level it unwinds through, at a cost of about 10 KB of stack per
/// level, so an error thrown a few hundred levels down overflows the stack, which kills the test process. Handled where it is
/// thrown, it unwinds no further.
/// </remarks>
class FirstSerializationError
{
   public FirstSerializationError(JsonSerializer serializer) => serializer.Error += Handle;

   /// <summary>The failure the first error describes, or null when there was none.</summary>
   public SerializationFailure? Failure { get; private set; }

   void Handle(object? sender, ErrorEventArgs arguments)
   {
      Failure ??= SerializationFailure.From(arguments.ErrorContext);
      arguments.ErrorContext.Handled = true;
   }
}
