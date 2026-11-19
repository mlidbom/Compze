using Compze.Abstractions;

namespace Compze.Teventive.TaggregateStores._internal;

///<summary>No taggregate store holds the taggregate asked for.</summary>
///<remarks>An <see cref="ArgumentOutOfRangeException"/>, since the id asked for names no taggregate: that is what a consumer can catch,<br/>
/// the exception itself being internal.</remarks>
class TaggregateNotFoundException(TaggregateId taggregateId) : ArgumentOutOfRangeException("id", $"No taggregate store holds a taggregate with id {taggregateId}.");
