using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Typermedia.HandlerRegistration;

public static partial class TypermediaHandlerRegistrarWithDependencyInjectionSupportExtensions
{
   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>
   {
      @this.Registrar.ForTuery<TTuery, TResult>((tuery, _) => handler(tuery));
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TResult> handler) where TTuery : ITuery<TResult>
                                                   where TDependency1 : class
   {
      @this.Registrar.ForTuery<TTuery, TResult>((tuery, kernel) => handler(tuery, kernel.Resolve<TDependency1>()));
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TDependency2, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TDependency2, TResult> handler) where TTuery : ITuery<TResult>
                                                                 where TDependency1 : class
                                                                 where TDependency2 : class
   {
      @this.Registrar.ForTuery<TTuery, TResult>((tuery, kernel) => handler(tuery, kernel.Resolve<TDependency1>(), kernel.Resolve<TDependency2>()));
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TDependency2, TDependency3, TResult> handler) where TTuery : ITuery<TResult>
                                                                               where TDependency1 : class
                                                                               where TDependency2 : class
                                                                               where TDependency3 : class
   {
      @this.Registrar.ForTuery<TTuery, TResult>((tuery, kernel) => handler(tuery, kernel.Resolve<TDependency1>(), kernel.Resolve<TDependency2>(), kernel.Resolve<TDependency3>()));
      return @this;
   }

   public static TypermediaHandlerRegistrarWithDependencyInjectionSupport ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(
      this TypermediaHandlerRegistrarWithDependencyInjectionSupport @this,
      Func<TTuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TTuery : ITuery<TResult>
                                                                                              where TDependency1 : class
                                                                                              where TDependency2 : class
                                                                                              where TDependency3 : class
                                                                                              where TDependency4 : class
   {
      @this.Registrar.ForTuery<TTuery, TResult>((tuery, kernel) => handler(tuery, kernel.Resolve<TDependency1>(), kernel.Resolve<TDependency2>(), kernel.Resolve<TDependency3>(), kernel.Resolve<TDependency4>()));
      return @this;
   }
}
