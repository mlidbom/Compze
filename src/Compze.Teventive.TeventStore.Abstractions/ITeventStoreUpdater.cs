using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TeventStore.Abstractions;

///<summary>The session through which one unit of work gets, adds and deletes the taggregates of a tevent store.</summary>
///<remarks>urgent: Drop <see cref="IDisposable"/> from this interface. The session is scoped, so the unit of work's scope<br/>
/// disposes it. On the interface it invites a caller to dispose a session that the scope owns (Magnus, 2026-11-16).</remarks>
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