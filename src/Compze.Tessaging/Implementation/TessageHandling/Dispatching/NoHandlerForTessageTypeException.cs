using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

class NoHandlerForTessageTypeException(Type tommandType) : Exception(tommandType.GetFullNameCompilable());