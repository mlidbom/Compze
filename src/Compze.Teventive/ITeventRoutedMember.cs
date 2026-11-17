using Compze.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Teventive;

///<summary>A part of a class that its parent's <see cref="TeventRouting"/> routes every tevent of <typeparamref name="TTevent"/> to:<br/>
/// a component, an entity collection, or a shared-tomponent slot. The parent declares the route, and the part dispatches what it receives.<br/>
/// What lets the parent's declaration infer the tevent type from what its lambda returns.</summary>
public interface ITeventRoutedMember<TTevent> where TTevent : ITevent
{
   ///<summary>Dispatches <paramref name="wrapped"/> to this part's tevent appliers.</summary>
   [Obsolete(ObsoleteMessage.ForInternalUseOnly)] void DispatchInternal(IPublisherTevent<ITevent> wrapped);
}
