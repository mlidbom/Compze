using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions;

public interface ITeventStoreUpdater : IDisposable
{
   /// <summary>Loads a taggregate and tracks it for changes.</summary>
   TTaggregate Get<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate;

   /// <summary>Saves the taggregate.</summary>
   void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate;

   /// <summary>Tries to get the specified instance. Returns false and sets the result to null if the taggregate did not exist.</summary>
   bool TryGet<TTaggregate>(TaggregateId taggregateId, [NotNullWhen(true)]out TTaggregate? result) where TTaggregate : class, ITaggregate;

   /// <summary>Deletes a taggregate from the store.</summary>
   void Delete(TaggregateId taggregateId);
}