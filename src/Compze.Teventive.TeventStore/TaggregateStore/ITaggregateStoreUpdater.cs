using Compze.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Teventive.TaggregateStore;

///<summary>The session through which one unit of work changes the taggregates of a taggregate store: the counterpart of<br/>
/// <c>ITeventStoreUpdater</c> for taggregates kept as their state. Each taggregate it gets is a private copy for this unit of work.<br/>
/// The tevents published on them are dispatched through <c>IUnitOfWorkTeventPublisher</c> when the unit of work commits, and the<br/>
/// copies then become the committed instances that <see cref="ITaggregateStoreReader"/> serves.</summary>
public interface ITaggregateStoreUpdater
{
   ///<summary>This unit of work's copy of the taggregate <paramref name="id"/>, which it may change. Waits while another unit of<br/>
   /// work holds the taggregate.</summary>
   TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate;

   ///<summary>As <see cref="Get{TTaggregate}"/>, or false if the store holds no taggregate <paramref name="id"/>.</summary>
   bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ISnapshotTaggregate;

   ///<summary>Adds a new taggregate to the store when this unit of work commits, with the tevents it has published.</summary>
   void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ISnapshotTaggregate;

   ///<summary>Removes the taggregate <paramref name="id"/> from the store when this unit of work commits.</summary>
   void Delete(TaggregateId id);
}
