using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
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
            return @this.NewtonsoftSerializers();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
