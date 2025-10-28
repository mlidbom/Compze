using System;
using Compze.Core.Wiring.Testing.Internal;
using Compze.Serialization.Newtonsoft.Wiring;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

namespace Compze.Tessaging.Hosting.Testing.Wiring;

public static class TestingComponentRegistrarSerializer
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
