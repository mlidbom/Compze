using System;

namespace Compze.Persistence.EventStore;

class AttemptToSaveEmptyAggregateException(object value) : Exception($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.");