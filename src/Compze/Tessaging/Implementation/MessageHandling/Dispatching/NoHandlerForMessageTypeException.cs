using System;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.MessageHandling.Dispatching;

class NoHandlerForMessageTypeException(Type commandType) : Exception(commandType.GetFullNameCompilable());