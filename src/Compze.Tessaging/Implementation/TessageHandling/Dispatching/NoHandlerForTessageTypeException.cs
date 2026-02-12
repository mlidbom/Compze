using System;
using Compze.Utilities.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

public class NoHandlerForTessageTypeException(Type tommandType) : Exception(tommandType.GetFullNameCompilable());