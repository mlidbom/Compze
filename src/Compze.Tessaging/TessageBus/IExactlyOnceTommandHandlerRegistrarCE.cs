using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Convenience overloads for <see cref="IExactlyOnceTommandHandlerRegistrar"/> — the same shapes<br/>
/// <see cref="TessageBusHandlerRegistrarCE"/> gives the full registrar: extra lambda parameters are resolved from the unit of<br/>
/// work executing the tommand, or the handler takes just the tommand.</summary>
public static class IExactlyOnceTommandHandlerRegistrarCE
{
   extension(IExactlyOnceTommandHandlerRegistrar @this)
   {
      public IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, Task> handler) where TTommand : IExactlyOnceTommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand, TDependency1>(Func<TTommand, TDependency1, Task> handler) where TTommand : IExactlyOnceTommand
                                                                                                                                where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));
   }
}
