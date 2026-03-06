using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia;

public static partial class TypermediaHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
   {
      @this.Registrar.ForTommand(handler);
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                     where TDependency1 : class
   {
      @this.Registrar.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>()));
      return @this;
   }
}
