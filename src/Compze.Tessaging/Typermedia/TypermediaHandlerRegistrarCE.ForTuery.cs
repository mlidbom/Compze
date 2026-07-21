using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

public static partial class TypermediaHandlerRegistrarCE
{
   extension(TypermediaHandlerRegistrar @this)
   {
      public TypermediaHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>
         => @this.ForTuery<TTuery, TResult>((tuery, _) => handler(tuery));

      public TypermediaHandlerRegistrar ForTuery<TTuery, TDependency1, TResult>(Func<TTuery, TDependency1, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                             where TDependency1 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>()));

      public TypermediaHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TResult>(Func<TTuery, TDependency1, TDependency2, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                         where TDependency1 : class
                                                                                                                                                         where TDependency2 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));

      public TypermediaHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TResult>(Func<TTuery, TDependency1, TDependency2, TDependency3, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                                                    where TDependency1 : class
                                                                                                                                                                                    where TDependency2 : class
                                                                                                                                                                                    where TDependency3 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>(), scope.Resolve<TDependency3>()));

      public TypermediaHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(Func<TTuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                                                                                where TDependency1 : class
                                                                                                                                                                                                                where TDependency2 : class
                                                                                                                                                                                                                where TDependency3 : class
                                                                                                                                                                                                                where TDependency4 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>(), scope.Resolve<TDependency3>(), scope.Resolve<TDependency4>()));
   }
}
