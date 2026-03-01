using System;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

internal class NoHandlerException(Type tessageType) : Exception($"No handler registered for queuedTessageInformation type: {tessageType.FullName}");