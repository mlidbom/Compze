using System;

namespace Compze.Messaging.Buses.Implementation;

class NoHandlerException(Type messageType) : Exception($"No handler registered for queuedMessageInformation type: {messageType.FullName}");