using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>Thrown when a typermedia tessage is sent through a <see cref="ITypermediaRouter"/> and no connected endpoint declares a handler for its type. Public because it reaches client code, which must be able to catch it.</summary>
public class NoHandlerForTypermediaTypeException(Type type) : Exception(type.GetFullNameCompilable());
