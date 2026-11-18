using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>The session through which one unit of work changes taggregates, whichever store keeps each class: one for each unit<br/>
/// of work, resolved from its scope.<br/>
/// A taggregate got for update is the unit of work's private copy, which it may change, and the unit of work holds the<br/>
/// taggregate's lock until it completes, so another unit of work that gets it for update waits.<br/>
/// The tevents published on the copies are dispatched as each publish cascade ends, so participation handlers run synchronously,<br/>
/// inside the unit of work's transaction. When it commits, the copies become the committed instances that<br/>
/// <see cref="ITaggregateStoreReader"/> gets.</summary>
///<remarks>A taggregate is deleted by publishing a tevent that implements <see cref="Taggregates.Tevents.ITaggregateDeletedTevent"/>,<br/>
/// from one of its own methods, as any other change is made. From then on the unit of work no longer finds it, and when the unit of<br/>
/// work commits it leaves its store.</remarks>
///<remarks>A store that keeps taggregates as their history, the tevent store, makes no copy: it loads the taggregate from its<br/>
/// history for the unit of work, and the database's lock stands for the taggregate's.</remarks>
///<remarks>One unit of work may change taggregates of several stores, and they commit together. But the stores commit one after the<br/>
/// other, and Compze can't recover what a crash between two commits leaves. So a unit of work that writes a state store kept in files<br/>
/// can't also hold a database connection, such as the tevent store's, unless the store's files allow it: see<br/>
/// <c>StateStoreBuilder.StoredInJsonFiles</c> and <c>TaggregateFilesBuilder.AcceptingNonAtomicCommitsWithADatabase</c>, in<br/>
/// Compze.Teventive.StateStore.</remarks>
public interface ITaggregateStoreUpdater : ITaggregateStoreReader
{
   ///<summary>This unit of work's copy of the taggregate <paramref name="id"/>, which it may change. Waits while another unit of<br/>
   /// work holds the taggregate, and throws if no store holds it.</summary>
   TTaggregate GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="GetForUpdate{TTaggregate}"/>, or false if no store holds the taggregate <paramref name="id"/>. The unit<br/>
   /// of work holds its lock either way, so no other unit of work can add it before this one completes.</summary>
   bool TryGetForUpdate<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>Adds <paramref name="taggregate"/>, new and not yet in any store, to the store that keeps its class when this unit<br/>
   /// of work commits. The tevents it has published are dispatched now, in the order published.</summary>
   void Add(ITaggregate taggregate);
}
