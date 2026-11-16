using Compze.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Compze.Teventive.TaggregateStore;

///<summary>Reads the committed taggregates of a taggregate store, outside any unit of work and at no cost: what it returns is the<br/>
/// committed instance itself, which is read only. To change a taggregate, get it from an <see cref="ITaggregateStoreUpdater"/>.</summary>
public interface ITaggregateStoreReader
{
   ///<summary>The committed instance of the taggregate <paramref name="id"/>.</summary>
   TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate;

   ///<summary>As <see cref="Get{TTaggregate}"/>, or false if the store holds no taggregate <paramref name="id"/>.</summary>
   bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ISnapshotTaggregate;

   ///<summary>The reference that stands for the taggregate <paramref name="id"/>: what to hold across units of work.</summary>
   ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ISnapshotTaggregate;
}
