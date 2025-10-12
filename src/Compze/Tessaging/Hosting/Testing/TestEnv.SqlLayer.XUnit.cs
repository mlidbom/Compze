using System;
using System.Diagnostics.CodeAnalysis;
using Compze.Utilities.SystemCE;
using static Compze.Utilities.Contracts.Assert;

namespace Compze.Tessaging.Hosting.Testing;

static partial class TestEnv
{
   static class XUnit
   {
      public static class XUnitSqlLayer
      {
         static readonly LazyStruct<Wiring.SqlLayer> Cache = new LazyStruct<Wiring.SqlLayer>(GetCurrent);

         static Wiring.SqlLayer GetCurrent()
            => _xUnitPluggableComponentsCombination!.Value.SqlLayer;

         public static Wiring.SqlLayer Current => Cache.Value;

         [return: NotNull] static TValue SelectValue<TValue>(TValue value, string provider)
         {
            if(!Equals(value, default(TValue))) return Result.ReturnNotNull(value);

            throw new Exception($"Value missing for {provider}");
         }
      }

      public static class XUnitDIContainer
      {
         static readonly LazyStruct<Wiring.DIContainer> Cache = new(GetCurrent);
         public static Wiring.DIContainer Current => Cache.Value;

         static Wiring.DIContainer GetCurrent()
            => _xUnitPluggableComponentsCombination!.Value.DiContainer;
      }
   }
}
