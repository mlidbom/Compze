using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarSerializer
{
   public static IComponentRegistrar CurrentTestsSerializer(this IComponentRegistrar register) =>
      register.CastTo<TestingComponentRegistrar>().CurrentTestsSerializers();

   public static IComponentRegistrar CurrentTestsSerializers(this TestingComponentRegistrar @this)
   {
      switch(TestEnv.Serializer)
      {
         case Serializer.Newtonsoft:
            return @this.NewtonsoftSerializers();
         default:
            throw new ArgumentOutOfRangeException();
      }
   }
}
