using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

///<summary>Convenience overloads for <see cref="ITessageHandlerRegistrar"/>: register a handler whose extra lambda parameters are<br/>
/// resolved from the handling unit of work, or one that needs no resolutions at all — instead of taking the<br/>
/// <c>IUnitOfWorkResolver</c> and resolving by hand.</summary>
public static class TessageHandlerRegistrarCE
{
   extension(ITessageHandlerRegistrar @this)
   {
      public ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent> handler) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => handler(tevent));

      public ITessageHandlerRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> handler) where TTevent : ITevent
                                                                                                              where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, kernel) => handler(tevent, kernel.Resolve<TDependency1>()));

      public ITessageHandlerRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Action<TTevent, TDependency1, TDependency2> handler) where TTevent : ITevent
                                                                                                                                          where TDependency1 : class
                                                                                                                                          where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, kernel) => handler(tevent, kernel.Resolve<TDependency1>(), kernel.Resolve<TDependency2>()));

      public ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand> handler) where TTommand : ITommand
         => @this.ForTommand<TTommand>((tommand, _) => handler(tommand));

      public ITessageHandlerRegistrar ForTommand<TTommand, TDependency1>(Action<TTommand, TDependency1> handler) where TTommand : ITommand
                                                                                                                 where TDependency1 : class
         => @this.ForTommand<TTommand>((tommand, kernel) => handler(tommand, kernel.Resolve<TDependency1>()));
   }
}
