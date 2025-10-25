using System;
using System.Collections.Generic;
using Compze.Abstractions.Time.Public;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public interface ITeventStored
{
   Guid Id { get; }
   int Version { get; }

   void Commit(Action<IReadOnlyList<IAggregateTevent>> commitTevents);
   void LoadFromHistory(IEnumerable<IAggregateTevent> history);
   void SetTimeSource(IUtcTimeTimeSource timeSource);
   IObservable<IAggregateTevent> TeventStream { get; }
}

public interface ITeventStored<out TTevent> : ITeventStored where TTevent : IAggregateTevent
{
   new IObservable<TTevent> TeventStream { get; }
}