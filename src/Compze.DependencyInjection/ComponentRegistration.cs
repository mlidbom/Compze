using System.Reflection;
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

   internal abstract ComponentRegistration CreateCloneRegistration(IRootResolver currentRootResolver);
   internal abstract ComponentRegistration CreateChildRegistration(IRootResolver parentRootResolver);

   bool _exposeServiceResolver;

   /// <summary>
   /// Opts this component in to deferred resolution: see <see cref="ComponentRegistration{TService}"/>'s <c>WithServiceResolver()</c>,
   /// which is the fluent entry point. Once opted in, <see cref="ServiceResolverRegistrations"/> yields the resolver registrations.
   /// </summary>
   private protected void ExposeServiceResolver() => _exposeServiceResolver = true;

   /// <summary>
   /// The additional registrations this component contributes to the container: one <see cref="IServiceResolver{TService}"/>
   /// registration for <em>each</em> of its <see cref="ServiceTypes"/> when opted in through <c>WithServiceResolver()</c>, otherwise none.<br/>
   /// Each resolver is registered at this component's own <see cref="Lifestyle"/>, so a dependency on it is subject to exactly the
   /// same lifestyle validation as a direct dependency on the component.
   /// </summary>
   internal IEnumerable<ComponentRegistration> ServiceResolverRegistrations() =>
      _exposeServiceResolver
         ? ServiceTypes.Select(serviceType => CreateServiceResolverRegistration(serviceType, Lifestyle)).ToList()
         : [];

   static ComponentRegistration CreateServiceResolverRegistration(Type serviceType, Lifestyle lifestyle) =>
      (ComponentRegistration)CreateTypedServiceResolverRegistrationDefinition
         .MakeGenericMethod(serviceType)
         .Invoke(obj: null, parameters: [lifestyle])!;

   static readonly MethodInfo CreateTypedServiceResolverRegistrationDefinition =
      typeof(ComponentRegistration).GetMethod(nameof(CreateTypedServiceResolverRegistration), BindingFlags.NonPublic | BindingFlags.Static)!;

   static ComponentRegistration<IServiceResolver<TServiceType>> CreateTypedServiceResolverRegistration<TServiceType>(Lifestyle lifestyle) where TServiceType : class =>
      new(lifestyle,
          serviceTypes: [typeof(IServiceResolver<TServiceType>)],
          InstantiationSpec.FromFactoryMethod(serviceResolver => new ServiceResolver<TServiceType>(serviceResolver), typeof(ServiceResolver<TServiceType>)),
          dependencyTypes: []);
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

   /// <summary>
   /// Also expose this component through an <see cref="IServiceResolver{TService}"/> for <em>each</em> of its <see cref="ComponentRegistration.ServiceTypes"/>,
   /// so other components can depend on a deferred, typed resolver for it instead of the component itself — the supported way to
   /// break a constructor-injection cycle.
   /// </summary>
   /// <remarks>
   /// Each resolver is registered at this component's own <see cref="ComponentRegistration.Lifestyle"/>. A dependency on a resolver
   /// is therefore subject to exactly the same lifestyle validation as a direct dependency on the component would be.
   /// </remarks>
   public ComponentRegistration<TService> WithServiceResolver()
   {
      ExposeServiceResolver();
      return this;
   }

   internal override ComponentRegistration CreateCloneRegistration(IRootResolver currentRootResolver)
   {
      if(!ShouldDelegateToParentWhenCloning)
      {
         return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec, DependencyTypes, AllowSingletonDependent, AllowScopedDependent);
      }

      Contract.State.Assert(Lifestyle == Lifestyle.Singleton, () => "Only Singletons can delegate to parent container when cloning, because otherwise both containers would attempt to dispose the component");
      return new ComponentRegistration<TService>(////Instance registrations are not disposed.
         lifestyle: Lifestyle.Singleton,
         serviceTypes: ServiceTypes,
         instantiationSpec: InstantiationSpec.FromInstance(currentRootResolver.Resolve<TService>()),
         dependencyTypes: DependencyTypes,
         allowSingletonDependent: AllowSingletonDependent,
         allowScopedDependent: AllowScopedDependent
      );
   }

   internal override ComponentRegistration CreateChildRegistration(IRootResolver parentRootResolver)
   {
      if(Lifestyle == Lifestyle.Singleton)
      {
         // Child containers delegate ALL singletons to the parent — same instance, not disposed by child.
         return new ComponentRegistration<TService>(
            lifestyle: Lifestyle.Singleton,
            serviceTypes: ServiceTypes,
            instantiationSpec: InstantiationSpec.FromInstance(parentRootResolver.Resolve<TService>()),
            dependencyTypes: DependencyTypes,
            allowSingletonDependent: AllowSingletonDependent,
            allowScopedDependent: AllowScopedDependent
         );
      }

      // Scoped and transient registrations are copied — fresh instances in child scopes.
      return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec, DependencyTypes, AllowSingletonDependent, AllowScopedDependent);
   }

   internal ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes,
                                  bool allowSingletonDependent = false,
                                  bool allowScopedDependent = false)
      : base(lifestyle, serviceTypes, instantiationSpec, dependencyTypes, allowSingletonDependent, allowScopedDependent) {}
}
