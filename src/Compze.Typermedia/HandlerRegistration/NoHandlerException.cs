namespace Compze.Typermedia.HandlerRegistration;

class NoHandlerException(Type tessageType) : Exception($"No handler registered for tessage type: {tessageType.FullName}");
