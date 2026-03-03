namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

class NoHandlerException(Type tessageType) : Exception($"No handler registered for queuedTessageInformation type: {tessageType.FullName}");