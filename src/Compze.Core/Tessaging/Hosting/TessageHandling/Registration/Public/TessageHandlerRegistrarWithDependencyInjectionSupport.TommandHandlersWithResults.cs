using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
   {
      @this.Register.ForTommand(handler);
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                     where TDependency1 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>()));
      return @this;
   }
}