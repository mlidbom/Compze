namespace Compze.Teventive.TaggregateStore;

///<summary>The taggregates of a store filed by keys their own state gives, such as a vocab by its question. Declared where the<br/>
/// store is composed, by a function from a taggregate to its keys, and kept by the store as units of work commit.</summary>
public interface ITaggregateIndex<TTaggregate, in TKey> where TTaggregate : class, ISnapshotTaggregate
{
   ///<summary>The taggregates filed under <paramref name="key"/>: the committed ones, or, inside a unit of work, as its own<br/>
   /// changes have left them.</summary>
   IReadOnlyList<ITaggregateReference<TTaggregate>> Lookup(TKey key);

   ///<summary>Whether any taggregate is filed under <paramref name="key"/>, as <see cref="Lookup"/> would find it. Outside a unit<br/>
   /// of work, or in one that holds no <typeparamref name="TTaggregate"/>, it allocates nothing.</summary>
   bool HasAny(TKey key);
}
