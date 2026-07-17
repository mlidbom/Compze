using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTessagingSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the Tessaging pipeline's serializer<br/>
   /// (<see cref="ITessagingSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTessagingSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(Private.Tessaging.NewtonsoftTessagingSerializer.RegisterWith);

   extension<TComposition>(TComposition @this) where TComposition : ITessagingSerializerSlot
   {
      ///<summary>Fills a feature composition's Tessaging-serializer slot (<see cref="ITessagingSerializerSlot"/>) with the Newtonsoft<br/>
      /// implementation — e.g. <c>AddExactlyOnceTessaging(tessaging => tessaging.NewtonsoftSerializer())</c>; see<br/>
      /// <see cref="NewtonsoftTessagingSerializer"/>, to which this delegates. Idempotent: a typermedia composition on the same<br/>
      /// endpoint may already have filled the slot, since the distributed substrate it composes serializes TessageBus<br/>
      /// conversations too.</summary>
      public TComposition NewtonsoftSerializer()
      {
         if(!@this.Registrar.IsRegistered<ITessagingSerializer>()) @this.Registrar.NewtonsoftTessagingSerializer();
         return @this;
      }
   }
}
