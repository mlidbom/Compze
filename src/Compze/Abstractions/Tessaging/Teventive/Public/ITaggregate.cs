using System;
using System.Collections.Generic;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.Core.Time.Public;

namespace Compze.Core.Tessaging.Teventive.Public;

public interface ITaggregate
{
   Guid Id { get; }
   int Version { get; }

   void Commit(Action<IReadOnlyList<ITaggregateTevent>> commitTevents);
   void LoadFromHistory(IEnumerable<ITaggregateTevent> history);
   void SetTimeSource(IUtcTimeTimeSource timeSource);
   IObservable<ITaggregateTevent> TeventStream { get; }
}

public interface ITaggregate<out TTevent> : ITaggregate where TTevent : ITaggregateTevent
{
   new IObservable<TTevent> TeventStream { get; }
}