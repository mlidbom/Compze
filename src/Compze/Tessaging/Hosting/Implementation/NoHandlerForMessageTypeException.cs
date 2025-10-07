using System;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Hosting.Implementation;

class NoHandlerForMessageTypeException(Type commandType) : Exception(commandType.GetFullNameCompilable());