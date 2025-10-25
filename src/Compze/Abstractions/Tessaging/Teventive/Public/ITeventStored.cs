using System;
using System.Collections.Generic;
using Compze.Abstractions.Time.Public;

namespace Compze.Abstractions.Tessaging.Teventive.Public;

public interface ITeventStored
{
   Guid Id { get; }
   int Version { get; }

   void Commit(Action<IReadOnlyList<ITaggregateTevent>> commitTevents);
   void LoadFromHistory(IEnumerable<ITaggregateTevent> history);
   void SetTimeSource(IUtcTimeTimeSource timeSource);
   IObservable<ITaggregateTevent> TeventStream { get; }
}

public interface ITeventStored<out TTevent> : ITeventStored where TTevent : ITaggregateTevent
{
   new IObservable<TTevent> TeventStream { get; }
}