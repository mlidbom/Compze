using System;
using System.Collections.Generic;
using Compze.GenericAbstractions.Time;

namespace Compze.Persistence.EventStore;

public interface IEventStored
{
   Guid Id { get; }
   int Version { get; }

   void Commit(Action<IReadOnlyList<IAggregateEvent>> commitEvents);
   void LoadFromHistory(IEnumerable<IAggregateEvent> history);
   void SetTimeSource(IUtcTimeTimeSource timeSource);
   IObservable<IAggregateEvent> EventStream { get; }
}

// ReSharper disable once UnusedTypeParameter it is used for type information in various parts of the framework.
public interface IEventStored<TEvent> : IEventStored where TEvent : IAggregateEvent;