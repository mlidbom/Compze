using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommand<TTommand>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTommand> handler) where TTommand : ITommand
   {
      @this.Registrar.ForTommand<TTommand>((tommand, _) => handler(tommand));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommand<TTommand, TDependency1>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                              where TDependency1 : class
   {
      @this.Registrar.ForTommand<TTommand>((tommand, kernel) => handler(tommand, kernel.Resolve<TDependency1>()));
      return @this;
   }
}