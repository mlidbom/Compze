using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Convenience overloads for <see cref="IBestEffortTeventHandlerRegistrar"/> — the same shapes<br/>
/// <see cref="TessageBusHandlerRegistrarCE"/> gives the full registrar, synchronous forms included: no subscription<br/>
/// registered here is exactly-once.</summary>
public static class IBestEffortTeventHandlerRegistrarCE
{
   extension(IBestEffortTeventHandlerRegistrar @this)
   {
      public IBestEffortTeventHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public IBestEffortTeventHandlerRegistrar ForTevent<TTevent>(Func<TTevent, Task> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public IBestEffortTeventHandlerRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                                                                                                       where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));

      public IBestEffortTeventHandlerRegistrar ForTevent<TTevent, TDependency1>(Func<TTevent, TDependency1, Task> handler) where TTevent : ITevent
                                                                                                                            where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));
   }
}
