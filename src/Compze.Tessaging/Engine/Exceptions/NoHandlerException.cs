using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Engine.HandlerRegistration.Internal;

namespace Compze.Tessaging.Engine.Exceptions;

///<summary>Thrown when a tessage of a single-handler kind — a tuery or a tommand, of either sibling style — reaches an engine<br/>
/// whose <see cref="TessageHandlerRoster"/> holds no handler for its type. The one language every lookup path speaks for a<br/>
/// missing handler, always naming the tessage type — never a raw dictionary failure.</summary>
public class NoHandlerException(Type tessageType) : Exception($"No handler registered for tessage type: {tessageType.FullName}");
