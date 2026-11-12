using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>Convenience overloads for <see cref="ITueryHandlerRegistrar"/> — the same shapes<br/>
/// <see cref="TypermediaHandlerRegistrarCE"/> gives the full registrar: extra lambda parameters are resolved from the<br/>
/// tuery's scope, or the handler takes just the tuery.</summary>
public static class ITueryHandlerRegistrarCE
{
   extension(ITueryHandlerRegistrar @this)
   {
      public ITueryHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>
         //refactor: this has no async counterpart taking a Func<TTuery, Task<TResult>>, so an async tuery handler that needs no dependency
         //   must take an IScopeResolver it doesn't use, as the tests' EndpointHostTestBase.BackendEndpoint tuery handler does. Typermedia
         //   tommand and best-effort tevent handlers have that form.
         => @this.ForTuery<TTuery, TResult>((tuery, _) => Task.FromResult(handler(tuery)));

      public ITueryHandlerRegistrar ForTuery<TTuery, TDependency1, TResult>(Func<TTuery, TDependency1, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                          where TDependency1 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => Task.FromResult(handler(tuery, scope.Resolve<TDependency1>())));

      public ITueryHandlerRegistrar ForTuery<TTuery, TDependency1, TDependency2, TResult>(Func<TTuery, TDependency1, TDependency2, TResult> handler) where TTuery : ITuery<TResult>
                                                                                                                                                      where TDependency1 : class
                                                                                                                                                      where TDependency2 : class
         => @this.ForTuery<TTuery, TResult>((tuery, scope) => Task.FromResult(handler(tuery, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>())));
   }
}
