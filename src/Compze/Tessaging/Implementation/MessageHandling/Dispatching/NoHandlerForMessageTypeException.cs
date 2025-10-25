using System;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

class NoHandlerForTessageTypeException(Type commandType) : Exception(commandType.GetFullNameCompilable());