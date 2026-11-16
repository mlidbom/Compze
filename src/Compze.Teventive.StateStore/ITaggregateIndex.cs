using Compze.Teventive.TaggregateStores;

namespace Compze.Teventive.StateStore;

///<summary>The taggregates of a state store filed under keys their own state gives, such as a vocab under its question. Declared<br/>
/// where the store is composed, by a function from a taggregate to its keys, and kept by the store as units of work commit.</summary>
///<remarks>Outside a unit of work a lookup reads the committed index, without a lock, and allocates nothing. Inside one it reads the<br/>
/// unit of work's own changes: a taggregate it holds a copy of is filed as the copy now is, not as it was committed.</remarks>
public interface ITaggregateIndex<out TTaggregate, in TKey> where TTaggregate : class, ITaggregate
{
   ///<summary>The taggregates filed under <paramref name="key"/>.</summary>
   IReadOnlyList<ITaggregateReference<TTaggregate>> Lookup(TKey key);

   ///<summary>Whether any taggregate is filed under <paramref name="key"/>, as <see cref="Lookup"/> would find it.</summary>
   bool HasAny(TKey key);
}
