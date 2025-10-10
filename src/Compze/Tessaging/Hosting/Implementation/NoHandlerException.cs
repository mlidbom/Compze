using System;

namespace Compze.Tessaging.Hosting.Implementation;

class NoHandlerException(Type messageType) : Exception($"No handler registered for queuedMessageInformation type: {messageType.FullName}");