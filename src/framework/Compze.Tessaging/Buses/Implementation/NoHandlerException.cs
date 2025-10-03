using System;

namespace Compze.Tessaging.Tessaging.Buses.Implementation;

class NoHandlerException(Type messageType) : Exception($"No handler registered for queuedMessageInformation type: {messageType.FullName}");