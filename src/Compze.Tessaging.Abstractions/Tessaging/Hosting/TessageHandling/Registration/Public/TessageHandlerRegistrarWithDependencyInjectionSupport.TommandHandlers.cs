using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommand<TTommand>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTommand> handler) where TTommand : ITommand
   {
      @this.Register.ForTommand(handler);
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommand<TTommand, TDependency1>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                              where TDependency1 : class
   {
      @this.Register.ForTommand<TTommand>(tommand => handler(tommand, @this.Resolve<TDependency1>()));
      return @this;
   }
}