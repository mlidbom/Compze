using Compze.DependencyInjection;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>Convenience overloads for <see cref="TeventObservationRegistrar"/> — the same shapes<br/>
/// <see cref="TessageBusHandlerRegistrarCE"/> gives <see cref="TessageBusHandlerRegistrar"/>: extra lambda parameters are resolved<br/>
/// from the observer's scope, or the observer takes just the tevent.</summary>
public static class TeventObservationRegistrarCE
{
   extension(TeventObservationRegistrar @this)
   {
      public TeventObservationRegistrar ForTevent<TTevent>(Action<TTevent> observer) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => observer(tevent));

      public TeventObservationRegistrar ForTevent<TTevent>(Func<TTevent, Task> observer) where TTevent : ITevent
         => @this.ForTevent<TTevent>((tevent, _) => observer(tevent));

      public TeventObservationRegistrar ForTevent<TTevent, TDependency1>(Action<TTevent, TDependency1> observer) where TTevent : ITevent
                                                                                                                 where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>()));

      public TeventObservationRegistrar ForTevent<TTevent, TDependency1>(Func<TTevent, TDependency1, Task> observer) where TTevent : ITevent
                                                                                                                     where TDependency1 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>()));

      public TeventObservationRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Action<TTevent, TDependency1, TDependency2> observer) where TTevent : ITevent
                                                                                                                                             where TDependency1 : class
                                                                                                                                             where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));

      public TeventObservationRegistrar ForTevent<TTevent, TDependency1, TDependency2>(Func<TTevent, TDependency1, TDependency2, Task> observer) where TTevent : ITevent
                                                                                                                                                 where TDependency1 : class
                                                                                                                                                 where TDependency2 : class
         => @this.ForTevent<TTevent>((tevent, scope) => observer(tevent, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));

      public TeventObservationRegistrar ForTeventsOfEachUnitOfWork<TTevent>(Action<IReadOnlyList<TTevent>> observer) where TTevent : ITevent
         => @this.ForTeventsOfEachUnitOfWork<TTevent>((tevents, _) => observer(tevents));

      public TeventObservationRegistrar ForTeventsOfEachUnitOfWork<TTevent>(Func<IReadOnlyList<TTevent>, Task> observer) where TTevent : ITevent
         => @this.ForTeventsOfEachUnitOfWork<TTevent>((tevents, _) => observer(tevents));

      public TeventObservationRegistrar ForTeventsOfEachUnitOfWork<TTevent, TDependency1>(Action<IReadOnlyList<TTevent>, TDependency1> observer) where TTevent : ITevent
                                                                                                                                      where TDependency1 : class
         => @this.ForTeventsOfEachUnitOfWork<TTevent>((tevents, scope) => observer(tevents, scope.Resolve<TDependency1>()));

      public TeventObservationRegistrar ForTeventsOfEachUnitOfWork<TTevent, TDependency1>(Func<IReadOnlyList<TTevent>, TDependency1, Task> observer) where TTevent : ITevent
                                                                                                                                          where TDependency1 : class
         => @this.ForTeventsOfEachUnitOfWork<TTevent>((tevents, scope) => observer(tevents, scope.Resolve<TDependency1>()));

      public TeventObservationRegistrar ForTeventsOfEachUnitOfWork<TTevent, TDependency1, TDependency2>(Action<IReadOnlyList<TTevent>, TDependency1, TDependency2> observer) where TTevent : ITevent
                                                                                                                                                                  where TDependency1 : class
                                                                                                                                                                  where TDependency2 : class
         => @this.ForTeventsOfEachUnitOfWork<TTevent>((tevents, scope) => observer(tevents, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));

      public TeventObservationRegistrar ForTeventsOfEachUnitOfWork<TTevent, TDependency1, TDependency2>(Func<IReadOnlyList<TTevent>, TDependency1, TDependency2, Task> observer) where TTevent : ITevent
                                                                                                                                                                      where TDependency1 : class
                                                                                                                                                                      where TDependency2 : class
         => @this.ForTeventsOfEachUnitOfWork<TTevent>((tevents, scope) => observer(tevents, scope.Resolve<TDependency1>(), scope.Resolve<TDependency2>()));
   }
}
