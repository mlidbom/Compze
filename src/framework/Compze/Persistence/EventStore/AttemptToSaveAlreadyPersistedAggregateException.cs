using System;

namespace Compze.Persistence.EventStore;

class AttemptToSaveAlreadyPersistedAggregateException : InvalidOperationException
{
   public AttemptToSaveAlreadyPersistedAggregateException(IEventStored aggregate)
      :base($"Instance of {aggregate.GetType() .FullName} with Id: {aggregate.Id} has already been persisted. To update it, load it from a session and modify it rather than attempting to call save")
   {

   }
}