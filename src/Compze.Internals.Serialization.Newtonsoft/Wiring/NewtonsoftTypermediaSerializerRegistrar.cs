using Compze.Abstractions.Serialization.Internal;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Internals.Serialization.Newtonsoft.Wiring;

public static class NewtonsoftTypermediaSerializerRegistrar
{
   ///<summary>Registers the Newtonsoft implementation of the Typermedia conversation's serializer<br/>
   /// (<see cref="ITypermediaSerializer"/>).</summary>
   public static IComponentRegistrar NewtonsoftTypermediaSerializer(this IComponentRegistrar registrar) =>
      registrar.Register(Private.Typermedia.NewtonsoftTypermediaSerializer.RegisterWith);

   extension<TComposition>(TComposition @this) where TComposition : ITypermediaSerializerSlot
   {
      ///<summary>Fills a feature composition's Typermedia-serializer slot (<see cref="ITypermediaSerializerSlot"/>) with the Newtonsoft<br/>
      /// implementation — e.g. <c>AddDistributedTypermedia(typermedia => typermedia.NewtonsoftSerializer())</c>; see<br/>
      /// <see cref="NewtonsoftTypermediaSerializer"/>, to which this delegates. The distributed substrate the typermedia pipeline<br/>
      /// composes serializes TessageBus conversations too, so declaring Newtonsoft here also fills the endpoint's<br/>
      /// Tessaging-serializer slot — unless a serializer was already declared for it.</summary>
      public TComposition NewtonsoftSerializer()
      {
         @this.Registrar.NewtonsoftTypermediaSerializer();
         if(!@this.Registrar.IsRegistered<ITessagingSerializer>()) @this.Registrar.NewtonsoftTessagingSerializer();
         return @this;
      }
   }
}
