using Compze.Teventive.Taggregates.Tevents.Public;

namespace Compze.Teventive.TeventStore.Abstractions.Internal;

interface ITeventStoreSerializer
{
   ///<summary>Serializes the whole wrapped tevent - the <see cref="ITaggregateTevent{TTeventInterface}"/> wrapper with its inner tevent inside - as one object graph.</summary>
   string Serialize(ITaggregateTevent<ITaggregateTevent> wrappedTevent);
   ///<summary>Deserializes a wrapped tevent: <paramref name="wrapperTeventType"/> is the closed wrapper type the tevent was persisted under.</summary>
   ITaggregateTevent<ITaggregateTevent> Deserialize(Type wrapperTeventType, string json);
}
