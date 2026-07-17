using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>The composition surface <c>AddDistributedTypermedia(typermedia => ...)</c>'s lambda receives: the slots distributed<br/>
/// Typermedia needs filled — currently the Typermedia serializer (<see cref="ITypermediaSerializerSlot"/>, filled by e.g.<br/>
/// <c>NewtonsoftSerializer()</c>). The slot-filling extension methods come from the implementation packages, so composing the<br/>
/// feature enumerates its choices right where they are made.</summary>
public class DistributedTypermediaComposition : ITypermediaSerializerSlot
{
   ///<summary>The endpoint's registrar, into which the slot-filling extensions register their implementations.</summary>
   public IComponentRegistrar Registrar { get; }

   public DistributedTypermediaComposition(IComponentRegistrar registrar) => Registrar = registrar;
}
