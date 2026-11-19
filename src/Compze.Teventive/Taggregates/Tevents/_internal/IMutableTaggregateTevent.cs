using Compze.Abstractions;

namespace Compze.Teventive.Taggregates.Tevents._internal;

///<summary>A taggregate tevent's values that its taggregate gives it when it publishes the tevent, and that a tevent store gives a tevent<br/>
/// it reads back: the taggregate's id, the version it is at after the tevent, and the time of the tevent.</summary>
interface IMutableTaggregateTevent : ITaggregateTevent
{
   void SetTaggregateIdInternal(TaggregateId taggregateId);
   void SetTaggregateVersionInternal(int taggregateVersion);
   void SetUtcTimeStampInternal(DateTime utcTimeStamp);
}
