using System.Text.Json.Serialization;
using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging;

///<summary>Identifies exactly one <see cref="ITessageWithIdentity"/>, generated when the tessage is created and never<br/>
/// modified afterwards. This is the identity the infrastructure deduplicates on, so it is what makes delivery guarantees<br/>
/// such as at-most-once and exactly-once expressible at all.</summary>
///<remarks><see cref="New"/> makes a version 7 <see cref="Guid"/> — time-ordered rather than random — so that<br/>
/// tessages inserted into the inbox and outbox arrive in roughly ascending key order instead of scattering across the<br/>
/// index.</remarks>
[JsonConverter(typeof(EntityIdJsonConverter))]
public class TessageId(Guid value) : EntityId(value)
{
   ///<summary>A new <see cref="TessageId"/>, of a new version 7 <see cref="Guid"/>.</summary>
   public new static TessageId New() => new(Guid.CreateVersion7());
}
