using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>The session through which one unit of work changes taggregates, whichever store keeps each class: resolved from the unit<br/>
/// of work's scope. Every scope of one transaction shares the transaction's instances of the taggregates.<br/>
/// A taggregate got for update, or added, is the unit of work's own instance, which it may change: no other unit of work sees the<br/>
/// changes before this one commits, and another unit of work that gets the same taggregate for update waits until this one completes.<br/>
/// The tevents published on the unit of work's taggregates are dispatched as each publish cascade ends, so participation handlers run<br/>
/// synchronously, inside the unit of work's transaction. When it commits, its changes are what <see cref="ITaggregateStoreReader"/> gets.</summary>
///<remarks><b>It needs a unit of work that <see cref="IScopeFactoryUnitOfWorkRunner.ExecuteUnitOfWork(IScopeFactory,Action{IUnitOfWorkResolver})"/><br/>
/// began:</b> one called with no ambient transaction, or one nested in such a unit of work. <see cref="GetForUpdate{TTaggregate}"/>,<br/>
/// <see cref="TryGetForUpdate{TTaggregate}"/> and <see cref="Add"/> throw outside a transaction, and inside one that something else<br/>
/// began, such as a <see cref="System.Transactions.TransactionScope"/> of the caller's own, an <c>ExecuteUnitOfWork</c> inside it<br/>
/// included. The tevents are dispatched in the scope of the unit of work that began the transaction, which lives until the<br/>
/// transaction completes, and only <c>ExecuteUnitOfWork</c> records which one that is. The reads need no unit of work.</remarks>
///<remarks><b>A publish that fails rolls the unit of work back,</b> even when the code that published catches the exception: an applier,<br/>
/// a participation handler or an invariant of the taggregate that throws leaves its state untrustworthy, so the unit of work refuses<br/>
/// to commit, and rolls back.</remarks>
///<remarks>A taggregate is deleted by publishing a tevent that implements <see cref="Taggregates.Tevents.ITaggregateDeletedTevent"/>,<br/>
/// from one of its own methods, as any other change is made. From then on the unit of work no longer finds it, and when the unit of<br/>
/// work commits it leaves its store.</remarks>
///<remarks>How a unit of work's instances are kept apart from the others', and how long a get for update waits, is each store's own.<br/>
/// The memory image state store copies the taggregate through its snapshot, and holds a lock of its own for it. The tevent store<br/>
/// builds the taggregate from its history, and the database's lock stands for the taggregate's. Each store's registrar method says<br/>
/// what that costs: <c>MemoryImageStateStoreRegistrar.MemoryImageStateStore</c> and <c>TeventStoreRegistrar.TeventStore</c>.</remarks>
///<remarks>One unit of work may change taggregates of several stores, and they commit together. But the stores commit one after the<br/>
/// other, and Compze can't recover what a crash between two commits leaves. So a unit of work that writes a memory image state store<br/>
/// kept in files can't also hold a database connection, such as the tevent store's, unless the store's files allow it: see<br/>
/// <c>MemoryImageStateStoreBuilder.StoredInJsonFiles</c> and <c>TaggregateFilesBuilder.AcceptingNonAtomicCommitsWithADatabase</c>, in<br/>
/// Compze.Teventive.MemoryImageStateStore.</remarks>
public interface ITaggregateStoreUpdater : ITaggregateStoreReader
{
   ///<summary>This unit of work's own instance of the taggregate <paramref name="id"/>, which it may change. Waits while another unit of<br/>
   /// work holds the taggregate, and throws an <see cref="ArgumentOutOfRangeException"/> if no store holds it.</summary>
   ///<remarks>As <see cref="ITaggregateStoreReader.Get{TTaggregate}"/> does, it throws for an id the store holds as a taggregate of<br/>
   /// another class. How long it waits, and what it costs, is the store's: see the store's registrar method.</remarks>
   TTaggregate GetForUpdate<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="GetForUpdate{TTaggregate}"/>, or false if the store that keeps <typeparamref name="TTaggregate"/> holds no<br/>
   /// taggregate <paramref name="id"/>. The unit of work holds its lock either way, so no other unit of work can add it before this one<br/>
   /// completes.</summary>
   bool TryGetForUpdate<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>Adds <paramref name="taggregate"/>, new and not yet in any store, to the store that keeps its class when this unit<br/>
   /// of work commits. The tevents it has published are dispatched now, in the order published.</summary>
   ///<remarks>Throws for an instance whose tevents an earlier add has taken, as in a unit of work that then rolled back: those tevents<br/>
   /// can't be dispatched again, so create the taggregate again.</remarks>
   void Add(ITaggregate taggregate);
}
