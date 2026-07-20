using Compze.Abstractions.Public;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging;

///<summary>Identifies exactly one <see cref="ITessageWithIdentity"/>, generated when the tessage is created and never<br/>
/// modified afterwards. This is the identity the infrastructure deduplicates on, so it is what makes delivery guarantees<br/>
/// such as at-most-once and exactly-once expressible at all.</summary>
///<remarks>The generating constructor produces a version 7 <see cref="Guid"/> — time-ordered rather than random — so that<br/>
/// tessages inserted into the inbox and outbox arrive in roughly ascending key order instead of scattering across the<br/>
/// index.</remarks>
public class TessageId(Guid id) : EntityId(id)
{
   public TessageId() : this(Guid.CreateVersion7()) {}
}
