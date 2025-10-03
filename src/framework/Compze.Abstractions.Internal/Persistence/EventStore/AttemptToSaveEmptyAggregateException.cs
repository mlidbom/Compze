using System;

namespace Compze.Abstractions.Internal.Persistence.EventStore;

class AttemptToSaveEmptyAggregateException(object value) : Exception($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.");