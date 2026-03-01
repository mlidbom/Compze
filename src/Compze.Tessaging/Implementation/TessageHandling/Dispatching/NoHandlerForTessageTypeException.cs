using System;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

internal class NoHandlerForTessageTypeException(Type tommandType) : Exception(tommandType.GetFullNameCompilable());