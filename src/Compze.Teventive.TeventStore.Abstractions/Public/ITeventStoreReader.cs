using Compze.Abstractions.Public;
using Compze.Teventive;
using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Public;

public interface ITeventStoreReader
{
   ///<summary>The taggregate's persisted history: its wrapped tevents exactly as published and stored, publisher identity included.</summary>
   IReadOnlyList<ITaggregateIdentifyingTevent<ITaggregateTevent>> GetHistory(TaggregateId taggregateId);
   /// <summary>
   /// Loads a specific version of the taggregate.
   /// This instance is NOT tracked for changes.
   /// No changes to this entity vill be persisted.
   /// </summary>
   TTaggregate GetReadonlyCopyOfVersion<TTaggregate>(TaggregateId taggregateId, int version) where TTaggregate : class, ITaggregate;

   TTaggregate GetReadonlyCopy<TTaggregate>(TaggregateId taggregateId) where TTaggregate : class, ITaggregate;
}
