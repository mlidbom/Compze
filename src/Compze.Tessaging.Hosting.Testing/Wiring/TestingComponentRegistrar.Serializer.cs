using Compze.Abstractions.Wiring.Testing.Internal;
using Compze.Internals.Serialization.Newtonsoft.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

static class TestingComponentRegistrarSerializer
{
   public static IComponentRegistrar CurrentTestsSerializersIfNotClonedContainer(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsSerializersIfNotClonedContainer();

   public static IComponentRegistrar CurrentTestsSerializersIfNotClonedContainer(this TestingComponentRegistrar @this)
   {
      if(@this.Container().IsClone())
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
