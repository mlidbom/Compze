using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Tessaging.TessageHandling.Registration.Public;

///<summary>Convenience overloads for <see cref="ITransactionIgnoringTeventHandlerRegistrar"/> — the same shapes<br/>
/// <see cref="TessageHandlerRegistrarCE"/> gives <see cref="ITessageHandlerRegistrar"/>: extra lambda parameters are resolved from<br/>
/// the handler's scope, or the handler takes just the tevent.</summary>
public static class TransactionIgnoringTeventHandlerRegistrarCE
{
   extension(ITransactionIgnoringTeventHandlerRegistrar @this)
   {
      public ITransactionIgnoringTeventHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public ITransactionIgnoringTeventHandlerRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                                                                                                                where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, kernel) => handler(tevent, kernel.Resolve<TDependency1>()));

      public ITransactionIgnoringTeventHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Action<TTevent, TDependency1, TDependency2> handler) where TTevent : ITevent
                                                                                                                                                            where TDependency1 : class
                                                                                                                                                            where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, kernel) => handler(tevent, kernel.Resolve<TDependency1>(), kernel.Resolve<TDependency2>()));
   }
}
