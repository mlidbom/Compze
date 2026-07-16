using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;

namespace Compze.DependencyInjection;

public abstract class ComponentRegistration
{
   public IReadOnlySet<Type> ServiceTypes { get; }
   public InstantiationSpec InstantiationSpec { get; }
   public Lifestyle Lifestyle { get; }

   /// <summary>
   /// Whether this registration joins a <see cref="IServiceResolverCE.ResolveSet{TComponent}(Abstractions.IServiceResolver)"/>-only
   /// component set (created via <c>ForSet(...)</c>) rather than being singularly resolvable via <c>Resolve&lt;T&gt;()</c>.
   /// </summary>
   /// <remarks>
   /// A service type is exclusively one or the other, container-wide: <see cref="ContainerBuilder"/> rejects a service type that
   /// is already registered the other way. Multiple set-member registrations may freely share the same service type — that
   /// multiplicity is the entire point of a component set — but a singular service type may never be registered twice.
   /// </remarks>
   public bool IsComponentSetMember { get; }

   internal IReadOnlyList<Type> DependencyTypes { get; }
   internal bool AllowSingletonDependent { get; }
   internal bool AllowScopedDependent { get; }
   internal bool ProvidesService(Type service) => ServiceTypes.Contains(service);

   protected ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes,
                                  bool isComponentSetMember,
                                  bool allowSingletonDependent = false,
                                  bool allowScopedDependent = false)
   {
      serviceTypes = [..serviceTypes];

      Contract.Argument.Assert(
         lifestyle == Lifestyle.Singleton || instantiationSpec.SingletonInstance == null,
         () => $"{nameof(InstantiationSpec.SingletonInstance)} registrations must be {nameof(Lifestyle.Singleton)}s");

      ServiceTypes = serviceTypes.ToHashSet();
      InstantiationSpec = instantiationSpec;
      Lifestyle = lifestyle;
      IsComponentSetMember = isComponentSetMember;
      DependencyTypes = [..dependencyTypes];
      AllowSingletonDependent = allowSingletonDependent;
      AllowScopedDependent = allowScopedDependent;
   }

   internal abstract ComponentRegistration CreateCloneRegistration(IRootResolver currentRootResolver);
   internal abstract ComponentRegistration CreateChildRegistration(IRootResolver parentRootResolver);

   /// <summary>
   /// The child-container registration for a singleton component set member: it wraps <paramref name="parentInstance"/> —
   /// same instance as the parent's, not disposed by the child — just like <see cref="CreateChildRegistration"/> does for a
   /// singular singleton.
   /// </summary>
   /// <remarks>
   /// A separate method because a set member cannot fetch its own instance the way a singular singleton can: singular
   /// resolution is forbidden for a component set's service type, so
   /// <see cref="DependencyInjectionContainer.CreateChildContainerBuilder"/> resolves the parent's whole set once and hands
   /// each member registration one instance from it through this method.
   /// </remarks>
   internal abstract ComponentRegistration CreateChildRegistrationDelegatingToParentInstance(object parentInstance);

   readonly List<ComponentRegistration> _associatedRegistrations = [];

   /// <summary>
   /// Extra registrations added to the container alongside this one when it is built.<br/>
   /// This is the general extension point behind fluent helpers such as <c>WithServiceResolver()</c>: a helper computes the
   /// companion registrations it needs and attaches them through the registration spec's
   /// (<see cref="ComponentRegistrationWithoutInstantiationSpec"/>) <c>WithAssociatedRegistrations()</c> modifiers; the spec's
   /// terminal call transfers them onto the finished registration, and the core needs no knowledge of what they are for.
   /// </summary>
   /// <remarks>
   /// Expanded recursively when the container is built: an associated registration may itself carry associated
   /// registrations, and every registration reachable this way is added to the container exactly once.
   /// </remarks>
   /// <remarks>
   /// <see cref="CreateCloneRegistration"/> and <see cref="CreateChildRegistration"/> deliberately do NOT copy this
   /// association: cloning starts from a built container, whose registration list was already expanded at build time,<br/>
   /// so the associated registrations are cloned as ordinary members of that list — copying the association too would
   /// expand them a second time in the clone and fail duplicate-registration validation.
   /// </remarks>
   internal IReadOnlyList<ComponentRegistration> AssociatedRegistrations => _associatedRegistrations;

   internal void AddAssociatedRegistrations(IEnumerable<ComponentRegistration> registrations) => _associatedRegistrations.AddRange(registrations);
}

public class ComponentRegistration<TService> : ComponentRegistration where TService : class
{
   readonly bool _shouldDelegateToParentWhenCloning;

   internal override ComponentRegistration CreateCloneRegistration(IRootResolver currentRootResolver)
   {
      if(!_shouldDelegateToParentWhenCloning)
      {
         return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec, DependencyTypes, IsComponentSetMember, AllowSingletonDependent, AllowScopedDependent);
      }

      return new ComponentRegistration<TService>(////Instance registrations are not disposed.
         lifestyle: Lifestyle.Singleton,
         serviceTypes: ServiceTypes,
         instantiationSpec: InstantiationSpec.FromInstance(currentRootResolver.Resolve<TService>()),
         dependencyTypes: DependencyTypes,
         isComponentSetMember: IsComponentSetMember,
         allowSingletonDependent: AllowSingletonDependent,
         allowScopedDependent: AllowScopedDependent
      );
   }

   internal override ComponentRegistration CreateChildRegistration(IRootResolver parentRootResolver)
   {
      if(Lifestyle == Lifestyle.Singleton)
      {
         Contract.State.Assert(!IsComponentSetMember,
            () => $"A singleton component set member cannot resolve its parent instance singularly — the child container creation must hand it one via {nameof(CreateChildRegistrationDelegatingToParentInstance)}.");

         // Child containers delegate ALL singletons to the parent — same instance, not disposed by child.
         return new ComponentRegistration<TService>(
            lifestyle: Lifestyle.Singleton,
            serviceTypes: ServiceTypes,
            instantiationSpec: InstantiationSpec.FromInstance(parentRootResolver.Resolve<TService>()),
            dependencyTypes: DependencyTypes,
            isComponentSetMember: IsComponentSetMember,
            allowSingletonDependent: AllowSingletonDependent,
            allowScopedDependent: AllowScopedDependent
         );
      }

      // Scoped and transient registrations are copied — fresh instances in child scopes.
      return new ComponentRegistration<TService>(Lifestyle, ServiceTypes, InstantiationSpec, DependencyTypes, IsComponentSetMember, AllowSingletonDependent, AllowScopedDependent);
   }

   internal override ComponentRegistration CreateChildRegistrationDelegatingToParentInstance(object parentInstance)
   {
      Contract.State.Assert(Lifestyle == Lifestyle.Singleton,
         () => $"Only singletons delegate to a parent container instance — {nameof(CreateChildRegistration)} copies the other lifestyles into the child.");

      return new ComponentRegistration<TService>(
         lifestyle: Lifestyle.Singleton,
         serviceTypes: ServiceTypes,
         instantiationSpec: InstantiationSpec.FromInstance((TService)parentInstance),
         dependencyTypes: DependencyTypes,
         isComponentSetMember: IsComponentSetMember,
         allowSingletonDependent: AllowSingletonDependent,
         allowScopedDependent: AllowScopedDependent
      );
   }

   internal ComponentRegistration(Lifestyle lifestyle,
                                  IEnumerable<Type> serviceTypes,
                                  InstantiationSpec instantiationSpec,
                                  IEnumerable<Type> dependencyTypes,
                                  bool isComponentSetMember,
                                  bool allowSingletonDependent = false,
                                  bool allowScopedDependent = false,
                                  bool delegateToParentServiceLocatorWhenCloning = false)
      : base(lifestyle, serviceTypes, instantiationSpec, dependencyTypes, isComponentSetMember, allowSingletonDependent, allowScopedDependent)
   {
      Contract.Argument.Assert(
         !delegateToParentServiceLocatorWhenCloning || lifestyle == Lifestyle.Singleton,
         () => "Only singletons can be delegated to parent container since disposal concern handling becomes very confused for any other lifestyle");
      _shouldDelegateToParentWhenCloning = delegateToParentServiceLocatorWhenCloning;
   }
}
