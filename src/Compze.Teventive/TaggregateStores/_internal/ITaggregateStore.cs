using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>What <see cref="ITaggregateStoreReader"/>, <see cref="ITaggregateStoreBulkReader"/> and <see cref="ITaggregateStoreUpdater"/> need<br/>
/// of a taggregate store: the taggregate classes it keeps, so that each class is routed to its store, the reading, and a session for<br/>
/// each unit of work.</summary>
///<remarks>Each store registers itself as a member of the component set of this interface, with
/// <see cref="TaggregateStoresRegistrar.RegisterTaggregateStore"/>.</remarks>
interface ITaggregateStore
{
   ///<summary>The store's name, for messages, such as "the state store".</summary>
   string Name { get; }

   ///<summary>The taggregate classes this store keeps, each declared where the store is composed.</summary>
   IReadOnlyList<Type> TaggregateClasses { get; }

   ///<summary>As <see cref="ITaggregateStoreReader.TryGet{TTaggregate}"/>, for the taggregates of this store.</summary>
   bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="ITaggregateStoreReader.Reference{TTaggregate}"/>, for the taggregates of this store.</summary>
   ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="ITaggregateStoreBulkReader.ReferencesToAll{TTaggregate}"/>, for the taggregates of this store.</summary>
   IReadOnlyCollection<ITaggregateReference<TTaggregate>> ReferencesToAll<TTaggregate>() where TTaggregate : class, ITaggregate;

   ///<summary>This store's part of <paramref name="unitOfWork"/>. Called once for each unit of work, when it first needs the store.</summary>
   ITaggregateStoreSession NewSession(TaggregateUnitOfWork unitOfWork);
}
