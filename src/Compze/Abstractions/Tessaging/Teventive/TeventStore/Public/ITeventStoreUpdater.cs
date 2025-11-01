using System;
using Compze.Core.Public;
using Compze.Core.Tessaging.Teventive.Public;

namespace Compze.Core.Tessaging.Teventive.TeventStore.Public;

public interface ITeventStoreUpdater : IDisposable
{
   /// <summary>
   /// Loads an taggregate and tracks it for changes.
   /// </summary>
   TTaggregate Get<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate;

   /// <summary>
   /// Causes the store to start tracking the taggregate.
   /// </summary>
   void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate;

   /// <summary>
   /// Tries to get the specified instance. Returns false and sets the result to null if the taggregate did not exist.
   /// </summary>
   bool TryGet<TTaggregate>(TaggregateId taggregateId, out TTaggregate? result) where TTaggregate : class, ITaggregate;

   /// <summary>
   /// Deletes all traces of an taggregate from the store.
   /// </summary>
   void Delete(TaggregateId taggregateId);
}