using Compze.Core.Tessaging.Public;

namespace Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;

public static partial class TessageHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>
   {
      @this.Register.ForTuery(handler);
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TResult> handler) where TTuery : ITuery<TResult>
                                                   where TDependency1 : class
   {
      @this.Register.ForTuery<TTuery, TResult>(tuery => handler(tuery, @this.Resolve<TDependency1>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TDependency2, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TDependency2, TResult> handler) where TTuery : ITuery<TResult>
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
   {
      @this.Register.ForTuery<TTuery, TResult>(tuery => handler(tuery, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>()));
      return @this;
   }

   public static TessageHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TResult>(
      this TessageHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TDependency2, TDependency3, TResult> handler) where TTuery : ITuery<TResult>
                                                                               where TDependency1 : class
                                                                               where TDependency2 : class
                                                                               where TDependency3 : class
   {
      @this.Register.ForTuery<TTuery, TResult>(tuery => handler(tuery, @this.Resolve<TDependency1>(), @this.Resolve<TDependency2>(), @this.Resolve<TDependency3>()));
      return @this;
   }
}
