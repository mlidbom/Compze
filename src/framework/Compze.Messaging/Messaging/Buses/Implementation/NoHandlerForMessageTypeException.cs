using System;

namespace Compze.Messaging.Buses.Implementation;

class NoHandlerForMessageTypeException(Type commandType) : Exception(commandType.FullName);