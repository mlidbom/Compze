using Compze.Teventive.TaggregateStores;

namespace Compze.Teventive.MemoryImageStateStore;

///<summary>The taggregates of a memory image state store filed under keys their own state gives, such as a vocab under its question. Declared<br/>
/// where the store is composed, by a function from a taggregate to its keys, and kept by the store as units of work commit.</summary>
///<remarks><b>One index for each pair of taggregate class and key type.</b> An index is known by its class and key type alone, as the<br/>
/// container resolves it, so a store has at most one index of a class by a key type, and two indexes of one class need key types of<br/>
/// their own, such as a type of its own for a vocab's question and another for its reading. An index of a class and one of a class<br/>
/// derived from it, by the same key type, are two indexes, each resolved by its own types.<br/>
/// <b>Its key function gives keys from the taggregate's own state alone,</b> the same keys each time for the same state, and changes<br/>
/// nothing: the store calls it when it loads, when a unit of work prepares to commit, and at lookups inside a unit of work, among<br/>
/// them on committed instances, which other threads read at the same time. The index keeps no record of a taggregate's keys, so a<br/>
/// commit takes the keys a taggregate leaves from its committed instance. A key function that reads anything else, such as another taggregate or a service,<br/>
/// fails the unit of work that changes the taggregate when it gives the committed instance a key the instance isn't filed under, and<br/>
/// that unit of work rolls back. A key it gives twice files the taggregate once.</remarks>
///<remarks><b>What a lookup costs.</b> Outside a unit of work, and inside one that holds no taggregate of the class, a lookup reads the<br/>
/// committed index, without a lock, and allocates nothing: it returns the index's own collection, which a caller must not cast to change.<br/>
/// Inside a unit of work that holds any taggregate of the class, got for update or added, changed or not, it reads the unit of work's<br/>
/// own changes: a taggregate it holds is filed as its instance now is, not as it was committed. Each lookup then builds a new<br/>
/// collection, and runs the key function on every taggregate of the class the unit of work holds, so it costs in proportion to them.<br/>
/// A commit that files a taggregate under a key, or takes it out of one, copies that key's whole collection: a key under which much of<br/>
/// the store is filed makes each such commit cost in proportion to what it files.</remarks>
///<remarks><b>For a moment during a commit, a lookup can disagree with the taggregates' state.</b> A commit makes the unit of work's<br/>
/// instances the committed ones first, and files them under their new keys after. In between, a lookup outside the unit of work can<br/>
/// find a taggregate under a key its new state no longer gives, or not yet find it under one it now gives. Code that must be sure<br/>
/// checks the state of what it found.</remarks>
public interface ITaggregateIndex<out TTaggregate, in TKey> where TTaggregate : class, ITaggregate
{
   ///<summary>The taggregates filed under <paramref name="key"/>, in no particular order.</summary>
   ///<remarks>No order, because a key may file a great many taggregates, and putting them in one would cost every lookup more than<br/>
   /// many callers spend on what they find. A caller that needs an order, or one of several by a rule of its own, sorts or chooses<br/>
   /// itself, after it has filtered.</remarks>
   IReadOnlyCollection<ITaggregateReference<TTaggregate>> Lookup(TKey key);
}
