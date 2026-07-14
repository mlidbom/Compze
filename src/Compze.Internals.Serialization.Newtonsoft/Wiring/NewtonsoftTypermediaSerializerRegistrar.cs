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
      /// <see cref="NewtonsoftTypermediaSerializer"/>, to which this delegates.</summary>
      public TComposition NewtonsoftSerializer()
      {
         @this.Registrar.NewtonsoftTypermediaSerializer();
         return @this;
      }
   }
}
