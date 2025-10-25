using System;
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

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TResult> handler) where TTommand : ITommand<TResult>
                                                                   where TDependency1 : class
                                                                   where TDependency2 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TResult> handler) where TTommand : ITommand<TResult>
                                                                                 where TDependency1 : class
                                                                                 where TDependency2 : class
                                                                                 where TDependency3 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TTommand : ITommand<TResult>
                                                                                               where TDependency1 : class
                                                                                               where TDependency2 : class
                                                                                               where TDependency3 : class
                                                                                               where TDependency4 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                             where TDependency1 : class
                                                                                                             where TDependency2 : class
                                                                                                             where TDependency3 : class
                                                                                                             where TDependency4 : class
                                                                                                             where TDependency5 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                           where TDependency1 : class
                                                                                                                           where TDependency2 : class
                                                                                                                           where TDependency3 : class
                                                                                                                           where TDependency4 : class
                                                                                                                           where TDependency5 : class
                                                                                                                           where TDependency6 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                         where TDependency1 : class
                                                                                                                                         where TDependency2 : class
                                                                                                                                         where TDependency3 : class
                                                                                                                                         where TDependency4 : class
                                                                                                                                         where TDependency5 : class
                                                                                                                                         where TDependency6 : class
                                                                                                                                         where TDependency7 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                                       where TDependency1 : class
                                                                                                                                                       where TDependency2 : class
                                                                                                                                                       where TDependency3 : class
                                                                                                                                                       where TDependency4 : class
                                                                                                                                                       where TDependency5 : class
                                                                                                                                                       where TDependency6 : class
                                                                                                                                                       where TDependency7 : class
                                                                                                                                                       where TDependency8 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>(), @this.Resolve<TDependency8>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTommandWithResult<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTommand, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                                                     where TDependency1 : class
                                                                                                                                                                     where TDependency2 : class
                                                                                                                                                                     where TDependency3 : class
                                                                                                                                                                     where TDependency4 : class
                                                                                                                                                                     where TDependency5 : class
                                                                                                                                                                     where TDependency6 : class
                                                                                                                                                                     where TDependency7 : class
                                                                                                                                                                     where TDependency8 : class
                                                                                                                                                                     where TDependency9 : class
   {
      @this.Register.ForTommand<TTommand, TResult>(tommand => handler(tommand, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>(), @this.Resolve<TDependency4>(), @this.Resolve<TDependency5>(), @this.Resolve<TDependency6>(), @this.Resolve<TDependency7>(), @this.Resolve<TDependency8>(), @this.Resolve<TDependency9>()));
      return @this;
   }
}