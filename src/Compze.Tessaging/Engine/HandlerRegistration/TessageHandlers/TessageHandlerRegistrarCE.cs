using Compze.DependencyInjection;

namespace Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;

///<summary>Convenience overloads for <see cref="TessageHandlerRegistrar"/>: register a handler whose extra lambda parameters are<br/>
/// resolved from the handling context — the unit of work for tevents and tommands, the scope for tueries — or one that needs no<br/>
/// resolutions at all, instead of taking the resolver and resolving by hand. Each delegates to the core verb of its shape, so<br/>
/// the synchrony rules (exactly-once kinds are async-only) apply identically here.</summary>
public static class TessageHandlerRegistrarCE
{
   extension(TessageHandlerRegistrar @this)
   {
      public TessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public TessageHandlerRegistrar ForTevent<TTevent>(Func<TTevent, Task> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public TessageHandlerRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                                                                                             where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));

      public TessageHandlerRegistrar ForTevent<TTevent, TDependency1>(Func<TTevent, TDependency1, Task> handler) where TTevent : ITevent
                                                                                                                 where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));

      public TessageHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Action<TTevent, TDependency1, TDependency2> handler) where TTevent : ITevent
                                                                                                                                         where TDependency1 : class
                                                                                                                                         where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>(), unitOfWork.Resolve<TDependency2>()));

      public TessageHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Func<TTevent, TDependency1, TDependency2, Task> handler) where TTevent : ITevent
                                                                                                                                             where TDependency1 : class
                                                                                                                                             where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>(), unitOfWork.Resolve<TDependency2>()));

      public TessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TessageHandlerRegistrar ForTommand<TTommand>(Func<TTommand, Task> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TessageHandlerRegistrar ForTommand<TTommand, TDependency1>(Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                                                                                                where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TessageHandlerRegistrar ForTommand<TTommand, TDependency1>(Func<TTommand, TDependency1, Task> handler) where TTommand : ITommand
                                                                                                                    where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TessageHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
         => @this.ForTommand<TTommand, TResult>((tommand, _) => handler(tommand));

      public TessageHandlerRegistrar ForTommand<TTommand, TDependency1, TResult>(Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                where TDependency1 : class
         => @this.ForTommand<TTommand, TResult>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public TessageHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>
         => @this.ForTuery<TTuery, TResult>((tuery, _) => handler(tuery));

      public TessageHandlerRegistrar ForTuery<TTuery, TDependency1, TResult>(Func<TTuery, TDependency1, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                          where TDependency1 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>()));

      public TessageHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TResult>(Func<TTuery, TDependency1, TDependency2, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                      where TDependency1 : class
                                                                                                                                                      where TDependency2 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));

      public TessageHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TResult>(Func<TTuery, TDependency1, TDependency2, TDependency3, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                                                 where TDependency1 : class
                                                                                                                                                                                 where TDependency2 : class
                                                                                                                                                                                 where TDependency3 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>(), scope.Resolve<TDependency3>()));

      public TessageHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult>(Func<TTuery, TDependency1, TDependency2, TDependency3, TDependency4, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                                                                             where TDependency1 : class
                                                                                                                                                                                                             where TDependency2 : class
                                                                                                                                                                                                             where TDependency3 : class
                                                                                                                                                                                                             where TDependency4 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>(), scope.Resolve<TDependency3>(), scope.Resolve<TDependency4>()));
   }
}
