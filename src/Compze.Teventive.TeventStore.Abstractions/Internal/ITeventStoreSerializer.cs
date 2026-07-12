using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Tessaging.Teventive.TeventStore.Internal;

public interface ITeventStoreSerializer
{
   ///<summary>Serializes the whole wrapped tevent - the <see cref="ITaggregateIdentifyingTevent{TTeventInterface}"/> wrapper with its inner tevent inside - as one object graph.</summary>
   string Serialize(ITaggregateIdentifyingTevent<ITaggregateTevent> wrappedTevent);
   ///<summary>Deserializes a wrapped tevent: <paramref name="wrapperTeventType"/> is the closed wrapper type the tevent was persisted under.</summary>
   ITaggregateIdentifyingTevent<ITaggregateTevent> Deserialize(Type wrapperTeventType, string json);
}
