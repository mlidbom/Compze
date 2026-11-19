using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores;

///<summary>Reads the taggregates that the taggregate stores of a container keep, whichever store keeps each class.<br/>
/// Outside a unit of work it gets a taggregate as last committed, as an instance that is read only: publishing on it throws.<br/>
/// Inside a unit of work it gets the unit of work's own instance of a taggregate it has got for update or added, so the unit of<br/>
/// work reads its own writes, and finds none it has deleted. To change a taggregate, get it for update from an<br/>
/// <see cref="ITaggregateStoreUpdater"/>.</summary>
///<remarks>What a read costs is the store's own. The memory image state store gives every reader the one instance it holds, with no<br/>
/// lock and no copy. The tevent store builds a new instance from the taggregate's history at each read, so two reads give two<br/>
/// instances. Each store's registrar method says what its reads cost: <c>MemoryImageStateStoreRegistrar.MemoryImageStateStore</c>, in<br/>
/// Compze.Teventive.MemoryImageStateStore, and <c>TeventStoreRegistrar.TeventStore</c>, in Compze.Teventive.TeventStore.</remarks>
public interface ITaggregateStoreReader
{
   ///<summary>The taggregate <paramref name="id"/>: the ambient unit of work's own instance if it holds one, else the taggregate as last<br/>
   /// committed. Throws an <see cref="ArgumentOutOfRangeException"/> if no store holds it.</summary>
   ///<remarks><typeparamref name="TTaggregate"/> picks the store to ask: the one that keeps that class, or classes derived from it. A class<br/>
   /// that no store of the container keeps throws, naming the stores and the classes they keep.<br/>
   /// <b>An id that the store holds as a taggregate of another class throws,</b> such as a vocab's id asked for as a sentence: that is a<br/>
   /// bug in the caller, not an absence.<br/>
   /// Asked for as a base class of its own class, the memory image state store gives the taggregate. The tevent store gives it only<br/>
   /// when one class alone of those declared for it derives from the base class, since a history doesn't say which class it is the<br/>
   /// history of: get a taggregate of the tevent store as its own class.</remarks>
   TTaggregate Get<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;

   ///<summary>As <see cref="Get{TTaggregate}"/>, or false if the store that keeps <typeparamref name="TTaggregate"/> holds no taggregate<br/>
   /// <paramref name="id"/>. It throws where <see cref="Get{TTaggregate}"/> throws for another reason than the absence: for an id the<br/>
   /// store holds as a taggregate of another class.</summary>
   bool TryGet<TTaggregate>(TaggregateId id, [NotNullWhen(true)] out TTaggregate? taggregate) where TTaggregate : class, ITaggregate;

   ///<summary>The <see cref="ITaggregateReference{TTaggregate}"/> that stands for the taggregate <paramref name="id"/>: what to hold<br/>
   /// across units of work, instead of an instance. It reads nothing, so it is made for any id, held or not: its<br/>
   /// <see cref="ITaggregateReference{TTaggregate}.Current"/> throws while no store holds the taggregate.</summary>
   ITaggregateReference<TTaggregate> Reference<TTaggregate>(TaggregateId id) where TTaggregate : class, ITaggregate;
}
