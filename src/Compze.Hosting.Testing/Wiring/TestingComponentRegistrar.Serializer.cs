using Compze.Serialization.Newtonsoft.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;
using Compze.Internals.Testing;

namespace Compze.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarSerializer
{
   ///<summary>Registers the serializers selected by the current test's pluggable-component configuration, unless this is a cloned container (clones inherit them from the root container).</summary>
   public static IComponentRegistrar CurrentTestsSerializersIfNotClonedContainer(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsSerializersIfNotClonedContainer();

   public static IComponentRegistrar CurrentTestsSerializersIfNotClonedContainer(this TestingComponentRegistrar @this)
   {
      if(@this.IsClone)
         return @this;

      switch(TestEnv.Serializer)
      {
         case Serializer.Newtonsoft:
            //The testing host may host endpoints speaking anything, so it registers every feature's serializer; each feature resolves only its own.
            return @this.NewtonsoftTessagingSerializer()
                        .NewtonsoftTypermediaSerializer()
                        .NewtonsoftDocumentDbSerializer()
                        .NewtonsoftTeventStoreSerializer();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }

   ///<summary>Registers the typermedia serializer of the current test's <see cref="Serializer"/>, and no other serializer:<br/>
   /// all a pure client's composition declares. A pure client composed for tests with more than an application's<br/>
   /// composition declares would hide what such a composition lacks.</summary>
   public static IComponentRegistrar CurrentTestsTypermediaSerializer(this IComponentRegistrar register) =>
      TestEnv.Serializer switch
      {
         Serializer.Newtonsoft => register.NewtonsoftTypermediaSerializer(),
         _                     => throw new ArgumentOutOfRangeException()
      };
}
