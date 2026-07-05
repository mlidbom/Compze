using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.ServiceBus.Implementation.TessageHandling.Dispatching;

class NoHandlerForTessageTypeException(Type tommandType) : Exception(tommandType.GetFullNameCompilable());