using Compze.Abstractions;
using Compze.Tessaging;
using Compze.Teventive.Taggregates.Tevents._internal;

namespace Compze.Teventive.Taggregates.Tevents;

///<summary>The base class of the tevents of a taggregate that a tevent store keeps: a <see cref="TaggregateTevent"/> with the<br/>
/// <see cref="Id"/> the store keeps it by.</summary>
public abstract class TaggregateTeventWithIdentity : TaggregateTevent, IMutableTaggregateTeventWithIdentity
{
   protected TaggregateTeventWithIdentity() {}
   protected TaggregateTeventWithIdentity(TaggregateId taggregateId) : base(taggregateId) {}

   ///<summary>Identifies this tevent: made when the tevent is created, and restored when a tevent store reads it back.</summary>
   public TessageId Id { get; private set; } = new();

   void IMutableTaggregateTeventWithIdentity.SetTessageIdInternal(TessageId tessageId) => Id = tessageId;
}
