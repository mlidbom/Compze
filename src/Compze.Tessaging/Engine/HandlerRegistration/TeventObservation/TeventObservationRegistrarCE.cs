using Compze.DependencyInjection;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;

namespace Compze.Tessaging.Engine.HandlerRegistration.TeventObservation;

///<summary>Convenience overloads for <see cref="TeventObservationRegistrar"/> — the same shapes<br/>
/// <see cref="TessageHandlerRegistrarCE"/> gives <see cref="TessageHandlerRegistrar"/>: extra lambda parameters are resolved<br/>
/// from the observer's scope, or the observer takes just the tevent.</summary>
public static class TeventObservationRegistrarCE
{
   extension(TeventObservationRegistrar @this)
   {
      public TeventObservationRegistrar ForTevent<TTevent>(Action<TTevent> observer) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => observer(tevent));

      public TeventObservationRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> observer) where TTevent : ITevent
                                                                                                                 where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>()));

      public TeventObservationRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Action<TTevent, TDependency1, TDependency2> observer) where TTevent : ITevent
                                                                                                                                             where TDependency1 : class
                                                                                                                                             where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));
   }
}
