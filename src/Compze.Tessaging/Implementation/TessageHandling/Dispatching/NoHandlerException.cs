using System;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

public class NoHandlerException(Type tessageType) : Exception($"No handler registered for queuedTessageInformation type: {tessageType.FullName}");