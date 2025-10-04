using System;

namespace Compze.Tessaging.Buses.Implementation;

class NoHandlerForMessageTypeException(Type commandType) : Exception(commandType.FullName);