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
///<remarks>A store that keeps taggregates as their history, the tevent store, makes no copy: it loads the taggregate from its<br/>
/// history for the unit of work, and the database's lock stands for the taggregate's.</remarks>
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
   ///<remarks>review-naming: Save only adds, so Add would tell the truth. For the naming pass (jastudio-rep-opus, 2026-11-16).</remarks>
   void Save<TTaggregate>(TTaggregate taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>Removes the taggregate <paramref name="id"/> from the store that keeps <typeparamref name="TTaggregate"/> when this unit<br/>
   /// of work commits.</summary>
   void Delete<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;
}
