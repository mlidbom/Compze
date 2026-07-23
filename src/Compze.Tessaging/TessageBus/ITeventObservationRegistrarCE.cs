using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Convenience overloads for <see cref="ITeventObservationRegistrar"/> — the same shapes<br/>
/// <see cref="TeventObservationRegistrarCE"/> gives the full registrar: extra lambda parameters are resolved from the<br/>
/// observer's scope, or the observer takes just the tevent.</summary>
public static class ITeventObservationRegistrarCE
{
   extension(ITeventObservationRegistrar @this)
   {
      public ITeventObservationRegistrar ForTevent<TTevent>(Action<TTevent> observer) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => observer(tevent));

      public ITeventObservationRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> observer) where TTevent : ITevent
                                                                                                                  where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>()));
   }
}
