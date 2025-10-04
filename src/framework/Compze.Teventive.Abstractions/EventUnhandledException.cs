using System;

namespace Compze.Teventive.Abstractions;

public class EventUnhandledException(Type handlerType, Type eventType) : Exception($"{handlerType} does not handle nor ignore incoming event {eventType}");
