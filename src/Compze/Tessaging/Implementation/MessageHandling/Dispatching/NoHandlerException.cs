using System;

namespace Compze.Tessaging.Implementation.MessageHandling.Dispatching;

class NoHandlerException(Type messageType) : Exception($"No handler registered for queuedMessageInformation type: {messageType.FullName}");