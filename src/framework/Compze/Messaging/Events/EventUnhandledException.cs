using System;

namespace Compze.Messaging.Events;

public class EventUnhandledException(Type handlerType, Type eventType) : Exception($"{handlerType} does not handle nor ignore incoming event {eventType}");
