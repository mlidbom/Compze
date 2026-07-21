using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

public static partial class TessageBusHandlerRegistrarCE
{
   extension(TessageBusHandlerRegistrar @this)
   {
      public TessageBusHandlerRegistrar ForTommand<TTommand>(Func<TTommand, Task> handler) where TTommand : IExactlyOnceTommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public TessageBusHandlerRegistrar ForTommand<TTommand, TDependency1>(Func<TTommand, TDependency1, Task> handler) where TTommand : IExactlyOnceTommand
                                                                                                                       where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, unitOfWork) => handler(tommand, unitOfWork.Resolve<TDependency1>()));
   }
}
