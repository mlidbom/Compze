using System;

namespace Compze.Persistence.EventStore;

class AttemptToSaveEmptyAggregateException : Exception
{
   public AttemptToSaveEmptyAggregateException(object value):base($"Attempting to save an: {value.GetType().FullName} that Version=0 and no history to persist.")
   {
   }
}