using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Serialization.Internal;

///<summary>A composition surface with a Typermedia-serializer slot: implemented by the composition a feature that speaks<br/>
/// Typermedia hands its compose lambda, so that serializer packages can offer their implementation of<br/>
/// <see cref="ITypermediaSerializer"/> right where the feature is composed —<br/>
/// e.g. <c>AddDistributedTypermedia(typermedia => typermedia.NewtonsoftSerializer())</c>.</summary>
public interface ITypermediaSerializerSlot
{
   ///<summary>The endpoint's registrar, into which the slot-filling extension registers the chosen serializer.</summary>
   IComponentRegistrar Registrar { get; }
}
