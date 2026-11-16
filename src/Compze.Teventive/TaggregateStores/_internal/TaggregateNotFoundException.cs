using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>No taggregate store holds the taggregate asked for.</summary>
///<remarks>refactor: <c>Compze.Teventive.TeventStore.Abstractions._internal.Exceptions.TaggregateNotFoundException</c> is the same<br/>
/// exception, for the tevent store. It goes when the tevent store joins <see cref="ITaggregateStoreUpdater"/>, in step 2b of the<br/>
/// spike (jastudio-rep-opus, 2026-11-16).</remarks>
class TaggregateNotFoundException(TaggregateId taggregateId) : Exception($"No taggregate store holds a taggregate with id {taggregateId}.");
