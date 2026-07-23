using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Convenience overloads for <see cref="IExactlyOnceTeventHandlerRegistrar"/> — the same shapes<br/>
/// <see cref="TessageBusHandlerRegistrarCE"/> gives the full registrar, async only: exactly-once kinds are async end to end.</summary>
public static class IExactlyOnceTeventHandlerRegistrarCE
{
   extension(IExactlyOnceTeventHandlerRegistrar @this)
   {
      public IExactlyOnceTeventHandlerRegistrar ForTevent<TTevent>(Func<TTevent, Task> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public IExactlyOnceTeventHandlerRegistrar ForTevent<TTevent, TDependency1>(Func<TTevent, TDependency1, Task> handler) where TTevent : ITevent
                                                                                                                             where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>()));

      public IExactlyOnceTeventHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Func<TTevent, TDependency1, TDependency2, Task> handler) where TTevent : ITevent
                                                                                                                                                         where TDependency1 : class
                                                                                                                                                         where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, unitOfWork) => handler(tevent, unitOfWork.Resolve<TDependency1>(), unitOfWork.Resolve<TDependency2>()));
   }
}
