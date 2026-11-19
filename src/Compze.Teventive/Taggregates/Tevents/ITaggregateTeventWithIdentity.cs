using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive.Taggregates.Tevents;

///<summary>A <see cref="ITaggregateTevent"/> with an identity of its own: the <see cref="ITessageWithIdentity.Id"/> that a tevent store keeps it by.</summary>
///<remarks>A tevent store requires it of every tevent of the taggregates it keeps, and so do Tessaging's delivery guarantees, through<br/>
/// <see cref="ITessageWithIdentity"/>. A taggregate whose tevents are only published within the process needs none.</remarks>
public interface ITaggregateTeventWithIdentity : ITaggregateTevent, ITessageWithIdentity;
