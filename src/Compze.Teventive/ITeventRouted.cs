using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive;

///<summary>A component that has a <see cref="TeventRouting"/> of its own, for tevents of <typeparamref name="TTevent"/>.<br/>
/// What lets <see cref="TeventRouting{TInstance}.Declaration.Component{TComponentTevent}"/> infer the component's tevent type.</summary>
// ReSharper disable once UnusedTypeParameter It carries the component's tevent type for type inference.
public interface ITeventRouted<TTevent> where TTevent : ITevent
{
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] TeventRouting? TeventAppliersRoutingInternal { get; }
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] TeventRouting? TeventHandlersRoutingInternal { get; }
}
