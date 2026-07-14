using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Hosting;

///<summary>The composition surface <c>AddDistributedTessaging(tessaging => ...)</c>'s lambda receives: the slots distributed<br/>
/// Tessaging needs filled — currently the Tessaging serializer (<see cref="ITessagingSerializerSlot"/>, filled by e.g.<br/>
/// <c>NewtonsoftSerializer()</c>). The slot-filling extension methods come from the implementation packages, so composing the<br/>
/// feature enumerates its choices right where they are made.</summary>
public class DistributedTessagingComposition : ITessagingSerializerSlot
{
   ///<summary>The endpoint's registrar, into which the slot-filling extensions register their implementations.</summary>
   public IComponentRegistrar Registrar { get; }

   public DistributedTessagingComposition(IComponentRegistrar registrar) => Registrar = registrar;
}
