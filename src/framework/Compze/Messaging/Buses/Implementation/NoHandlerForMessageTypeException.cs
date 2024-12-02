using System;

namespace Compze.Messaging.Buses.Implementation;

public class NoHandlerForMessageTypeException : Exception
{
   public NoHandlerForMessageTypeException(Type commandType) : base(commandType.FullName) {}
}