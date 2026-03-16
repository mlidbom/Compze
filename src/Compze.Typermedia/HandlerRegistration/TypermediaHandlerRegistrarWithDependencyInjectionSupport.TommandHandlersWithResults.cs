using Compze.Abstractions.Tessaging.Public;

namespace Compze.Typermedia.HandlerRegistration;

public static partial class TypermediaHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
   {
      @this.Registrar.ForTommand<TTommand, TResult>((tommand, _) => handler(tommand));
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                     where TDependency1 : class
   {
      @this.Registrar.ForTommand<TTommand, TResult>((tommand, kernel) => handler(tommand, kernel.Resolve<TDependency1>()));
      return @this;
   }
}
