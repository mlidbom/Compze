using System;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public class EventUnhandledException(Type handlerType, Type eventType) : Exception($"{handlerType} does not handle nor ignore incoming event {eventType}");
