using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>Reads the taggregates that the taggregate stores of a container keep, whichever store keeps each class.<br/>
/// Outside a unit of work it gets the committed instance, which is read only and costs no lock and no copy.<br/>
/// Inside a unit of work it gets the unit of work's own copy of a taggregate it has got for update, so the unit of work reads<br/>
/// its own writes. To change a taggregate, get it for update from an <see cref="ITaggregateStoreUpdater"/>.</summary>
public interface ITaggregateStoreReader
{
   ///<summary>The taggregate <paramref name="id"/>: the ambient unit of work's copy if it holds one, else the committed instance.<br/>
   /// Throws if no store holds it.</summary>
   TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="Get{TTaggregate}"/>, or false if no store holds the taggregate <paramref name="id"/>.</summary>
   bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>The <see cref="ITaggregateReference{TTaggregate}"/> that stands for the taggregate <paramref name="id"/>: what to hold<br/>
   /// across units of work, instead of an instance.</summary>
   ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;
}
