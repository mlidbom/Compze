using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Typermedia.Client;

class NoHandlerForTypermediaTypeException(Type type) : Exception(type.GetFullNameCompilable());
