using System;
using System.Collections.Generic;
using Compze.Abstractions.Time.Public;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public interface IEventStored
{
   Guid Id { get; }
   int Version { get; }

   void Commit(Action<IReadOnlyList<IAggregateTevent>> commitEvents);
   void LoadFromHistory(IEnumerable<IAggregateTevent> history);
   void SetTimeSource(IUtcTimeTimeSource timeSource);
   IObservable<IAggregateTevent> EventStream { get; }
}

public interface IEventStored<out TEvent> : IEventStored where TEvent : IAggregateTevent
{
   new IObservable<TEvent> EventStream { get; }
}