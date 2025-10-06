using System;

namespace Compze.Tessaging.Hosting.Implementation;

class NoHandlerForMessageTypeException(Type commandType) : Exception(commandType.FullName);