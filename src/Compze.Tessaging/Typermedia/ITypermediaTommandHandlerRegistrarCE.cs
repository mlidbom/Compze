using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia;

///<summary>Convenience overloads for <see cref="ITypermediaTommandHandlerRegistrar"/> — the same shapes<br/>
/// <see cref="TypermediaHandlerRegistrarCE"/> gives the full registrar: extra lambda parameters are resolved from the unit of<br/>
/// work executing the tommand, or the handler takes just the tommand.</summary>
public static class ITypermediaTommandHandlerRegistrarCE
{
   extension(ITypermediaTommandHandlerRegistrar @this)
   {
      public ITypermediaTommandHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) =>
         {
            handler(tommand);
            return Task.CompletedTask;
         });

      public ITypermediaTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, Task> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public ITypermediaTommandHandlerRegistrar ForTommand<TTommand, TDependency1>(Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                                                                                                           where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) =>
         {
            handler(tommand, unitOfWork.Resolve<TDependency1>());
            return Task.CompletedTask;
         });

      public ITypermediaTommandHandlerRegistrar ForTommand<TTommand, TDependency1>(Func<TTommand, TDependency1, Task> handler) where TTommand : ITommand
                                                                                                                                where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));

      public ITypermediaTommandHandlerRegistrar ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) where TTommand : ITommand<TResult>
         => @this.ForTommand<TTommand, TResult>((tommand, _) => Task.FromResult(handler(tommand)));

      public ITypermediaTommandHandlerRegistrar ForTommand<TTommand, TDependency1, TResult>(Func<TTommand, TDependency1, TResult> handler) where TTommand : ITommand<TResult>
                                                                                                                                            where TDependency1 : class
         => @this.ForTommand<TTommand, TResult>((tommand, unitOfWork) => Task.FromResult(handler(tommand, unitOfWork.Resolve<TDependency1>())));
   }
}
