using System;

namespace Compze.Tessaging.Tessaging.Buses.Implementation;

class NoHandlerForMessageTypeException(Type commandType) : Exception(commandType.FullName);