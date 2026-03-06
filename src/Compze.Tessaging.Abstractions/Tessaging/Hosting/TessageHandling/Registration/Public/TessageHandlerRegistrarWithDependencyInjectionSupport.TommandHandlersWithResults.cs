using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
   {
      @this.TypermediaRegistrar.ForTommand(handler);
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                     where TDependency1 : class
   {
      @this.TypermediaRegistrar.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>()));
      return @this;
   }
}