using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>Convenience overloads for <see cref="TypermediaHandlerRegistrar"/>: register a handler whose extra lambda parameters<br/>
/// are resolved from the handling context — the unit of work for tommands, the scope for tueries — or one that needs no<br/>
/// resolutions at all, instead of taking the resolver and resolving by hand. Each delegates to the core verb of its shape.</summary>
public static class TypermediaHandlerRegistrarCE
{
   extension(TypermediaHandlerRegistrar @this)
   {
      public TypermediaHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TypermediaHandlerRegistrar ForTommand<TTommand>(Func<TTommand, Task> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TDependency1>(Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                                                                                                   where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TDependency1>(Func<TTommand, TDependency1, Task> handler) where TTommand : ITommand
                                                                                                                       where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
         => @this.ForTommand<TTommand, TResult>((tommand, _) => handler(tommand));

      public TypermediaHandlerRegistrar ForTommand<TTommand, TDependency1, TResult>(Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                   where TDependency1 : class
         => @this.ForTommand<TTommand, TResult>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

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
