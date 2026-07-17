namespace Compze.Tessaging.Typermedia.HandlerRegistration;

///<summary>Thrown when a typermedia tessage reaches an endpoint whose <see cref="TypermediaHandlerRegistry"/> has no handler<br/>
/// registered for its type — the one language every lookup speaks for a missing handler, local and remote alike.</summary>
public class NoHandlerException(Type tessageType) : Exception($"No handler registered for tessage type: {tessageType.FullName}");
