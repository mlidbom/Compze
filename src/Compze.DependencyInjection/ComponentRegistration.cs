using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public abstract class ComponentRegistration
{
   public IReadOnlySet<Type> ServiceTypes { get; }
   public InstantiationSpec InstantiationSpec { get; }
   public Lifestyle Lifestyle { get; }
   internal IReadOnlyList<Type> DependencyTypes { get; }
   internal bool AllowSingletonDependent { get; }
   internal bool AllowScopedDependent { get; }
   internal bool ProvidesService(Type service) => ServiceTypes.Contains(service);

   protected ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes,
                                  bool allowSingletonDependent = false,
                                  bool allowScopedDependent = false)
   {
      serviceTypes = serviceTypes.ToList();

      Contract.Argument.Assert(
         lifestyle == Lifestyle.Singleton || instantiationSpec.SingletonInstance == null,
         () => $"{nameof(InstantiationSpec.SingletonInstance)} registrations must be {nameof(Lifestyle.Singleton)}s");

      ServiceTypes = serviceTypes.ToHashSet();
      InstantiationSpec = instantiationSpec;
      Lifestyle = lifestyle;
      DependencyTypes = dependencyTypes.ToList();
      AllowSingletonDependent = allowSingletonDependent;
      AllowScopedDependent = allowScopedDependent;
   }

   internal abstract ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator);
}

public class ComponentRegistration<TService> : ComponentRegistration where TService : class
{
   bool ShouldDelegateToParentWhenCloning { get; set; }

   public ComponentRegistration<TService> DelegateToParentServiceLocatorWhenCloning()
   {
      Contract.State.Assert(
         Lifestyle == Lifestyle.Singleton,
         () => "Only singletons can be delegated to parent container since disposal concern handling becomes very confused for any other lifestyle");
      ShouldDelegateToParentWhenCloning = true;
      return this;
   }

   internal override ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator)
   {
      if(!ShouldDelegateToParentWhenCloning)
      {
         return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec, DependencyTypes, AllowSingletonDependent, AllowScopedDependent);
      }

      Contract.State.Assert(Lifestyle == Lifestyle.Singleton, () => "Only Singletons can delegate to parent container when cloning, because otherwise both containers would attempt to dispose the component");
      return new ComponentRegistration<TService>(////Instance registrations are not disposed.
         lifestyle: Lifestyle.Singleton,
         serviceTypes: ServiceTypes,
         instantiationSpec: InstantiationSpec.FromInstance(currentLocator.Resolve<TService>()),
         dependencyTypes: DependencyTypes,
         allowSingletonDependent: AllowSingletonDependent,
         allowScopedDependent: AllowScopedDependent
      );
   }

   internal ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes,
                                  bool allowSingletonDependent = false,
                                  bool allowScopedDependent = false)
      : base(lifestyle, serviceTypes, instantiationSpec, dependencyTypes, allowSingletonDependent, allowScopedDependent) {}
}
