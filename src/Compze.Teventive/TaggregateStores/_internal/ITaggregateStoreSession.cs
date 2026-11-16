using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>One store's part of a <see cref="TaggregateUnitOfWork"/>: the taggregates of that store the unit of work changes, and<br/>
/// what the store writes when the unit of work commits. The tevents its taggregates publish go to the unit of work, with<br/>
/// <see cref="TaggregateUnitOfWork.Collect"/>, which dispatches them.</summary>
interface ITaggregateStoreSession
{
   ///<summary>As <see cref="ITaggregateStoreUpdater.TryGetForUpdate{TTaggregate}"/>, for the taggregates of this store.</summary>
   bool TryGetForUpdate<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="ITaggregateStoreUpdater.Save{TTaggregate}"/>, for a taggregate of a class this store keeps.</summary>
   void Save(ITaggregate taggregate);

   ///<summary>As <see cref="ITaggregateStoreUpdater.Delete{TTaggregate}"/>, for the taggregates of this store.</summary>
   void Delete<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>Throws if a publish has failed on a taggregate of this session, whose state then can't be trusted: the unit of work<br/>
   /// rolls back, even if the code that published caught the exception.</summary>
   void AssertNoPublishHasFailed();

   ///<summary>Prepares this session's write, once every tevent of the unit of work is dispatched: everything that can fail is done<br/>
   /// here, so that <see cref="Commit"/> can't.</summary>
   void Prepare();

   ///<summary>Makes the prepared write, and the unit of work's taggregates the committed ones. Must not fail.</summary>
   void Commit();

   ///<summary>Discards what the unit of work did in this store.</summary>
   void Rollback();

   ///<summary>The unit of work has completed, committed or rolled back: releases what it held, such as the taggregates' locks.</summary>
   void Complete();
}
