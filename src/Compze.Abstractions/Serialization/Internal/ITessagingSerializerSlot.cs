using Compze.DependencyInjection.Abstractions;

namespace Compze.Abstractions.Serialization.Internal;

///<summary>A composition surface with a Tessaging-serializer slot: implemented by the composition a feature that stores and<br/>
/// transmits tessages hands its compose lambda, so that serializer packages can offer their implementation of<br/>
/// <see cref="ITessagingSerializer"/> right where the feature is composed —<br/>
/// e.g. <c>AddExactlyOnceTessaging(tessaging => tessaging.NewtonsoftSerializer())</c>.</summary>
public interface ITessagingSerializerSlot
{
   ///<summary>The endpoint's registrar, into which the slot-filling extension registers the chosen serializer.</summary>
   IComponentRegistrar Registrar { get; }
}
