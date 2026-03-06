using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia.HandlerRegistration;

public static partial class TypermediaHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTommand<TTommand>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTommand> handler) where TTommand : ITommand
   {
      @this.Registrar.ForTommand(handler);
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTommand<TTommand, TDependency1>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                              where TDependency1 : class
   {
      @this.Registrar.ForTommand<TTommand>(tommand => handler(tommand, @this.Resolve<TDependency1>()));
      return @this;
   }
}
