namespace Compze.Teventive.TaggregateStores;

///<summary>Reads every taggregate of a class, besides what <see cref="ITaggregateStoreReader"/> reads, whichever store keeps the class.</summary>
///<remarks>Reading every taggregate of a class is, for most stores, a scan of all they hold: the tevent store reads the creation tevent<br/>
/// of every history, and a store whose storage isn't all in memory reads all of it. So it has a reader of its own, and code that reads<br/>
/// everything says so in what it depends on, as code reading the document db does with its <c>IDocumentDbBulkReader</c>.</remarks>
public interface ITaggregateStoreBulkReader : ITaggregateStoreReader
{
   ///<summary>A reference to every <typeparamref name="TTaggregate"/> that the store keeping the class holds, in id order: the order of<br/>
   /// the ids' <see cref="Guid"/>s, which is the order of their text.</summary>
   ///<remarks>Inside a unit of work, the unit of work's view: the taggregates it has saved are included, and those it has deleted are<br/>
   /// left out. Outside one, the committed taggregates. A reference loads nothing until its<br/>
   /// <see cref="ITaggregateReference{TTaggregate}.Current"/> is read.<br/>
   /// review-naming: the document db's bulk reader says GetAll, and gives instances, where this gives references. For the naming pass<br/>
   /// (jastudio-rep-opus, 2026-11-17).</remarks>
   IReadOnlyList<ITaggregateReference<TTaggregate>> All<TTaggregate>() where TTaggregate : class, ITaggregate;
}
