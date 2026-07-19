using Compze.DependencyInjection.Runtime.Resolution;

namespace Compze.DependencyInjection.Wiring.Registration;

/// <summary>
/// The configuration phase of a fluent component registration: the component's service types, <see cref="Registration.Lifestyle"/>,
/// and policies, accumulated before the chain's terminal call — <c>CreatedBy(...)</c> or
/// <see cref="SingletonRegistrationWithoutInstantiationSpec{TService}.Instance"/> — builds the finished, immutable
/// <see cref="ComponentRegistration"/>. Every modifier comes before the terminal; nothing can be changed after it.
/// </summary>
/// <remarks>
/// This non-generic base exists so fluent modifiers that need no <c>TService</c> — such as
/// <see cref="ComponentRegistrationWithoutInstantiationSpecAssociatedRegistrationsExtensions"/>'s <c>WithAssociatedRegistrations()</c> and
/// <see cref="ComponentRegistrationWithoutInstantiationSpecServiceResolverExtensions"/>'s <c>WithServiceResolver()</c> — can be generic extensions constrained on it,
/// returning the concrete spec type so the chain keeps its full fluent surface.
/// </remarks>
public abstract class ComponentRegistrationWithoutInstantiationSpec
{
   internal Lifestyle Lifestyle { get; }
   internal IReadOnlyList<Type> ServiceTypes { get; }
   internal bool IsComponentSetMember { get; }
   internal bool SingletonDependentAllowed { get; private protected set; }
   internal bool ScopedDependentAllowed { get; private protected set; }
   internal bool ShouldDelegateToParentWhenCloning { get; private protected set; }

   readonly List<ComponentRegistration> _associatedRegistrations = [];
   readonly List<Func<ComponentRegistration, IEnumerable<ComponentRegistration>>> _createAssociatedRegistrationsWhenBuilt = [];

   private protected ComponentRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, bool isComponentSetMember)
   {
      Lifestyle = lifestyle;
      ServiceTypes = [..serviceTypes];
      IsComponentSetMember = isComponentSetMember;
   }

   internal void AddAssociatedRegistrations(IEnumerable<ComponentRegistration> registrations) => _associatedRegistrations.AddRange(registrations);
   internal void AddAssociatedRegistrationsCreatedWhenBuilt(Func<ComponentRegistration, IEnumerable<ComponentRegistration>> createAssociatedRegistrations) => _createAssociatedRegistrationsWhenBuilt.Add(createAssociatedRegistrations);

   private protected void AttachAssociatedRegistrations(ComponentRegistration builtRegistration)
   {
      builtRegistration.AddAssociatedRegistrations(_associatedRegistrations);
      _createAssociatedRegistrationsWhenBuilt.ForEach(create => builtRegistration.AddAssociatedRegistrations(create(builtRegistration)));
   }
}

/// <summary>
/// The typed registration spec for a component serving <typeparamref name="TService"/>. The <c>CreatedBy(...)</c> overloads in
/// <see cref="ComponentRegistrationExtensions"/> terminate the chain through this type, building the finished
/// <see cref="ComponentRegistration{TService}"/>.
/// </summary>
public class ComponentRegistrationWithoutInstantiationSpec<TService> : ComponentRegistrationWithoutInstantiationSpec where TService : class
{
   internal ComponentRegistrationWithoutInstantiationSpec(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, bool isComponentSetMember = false)
      : base(lifestyle, serviceTypes.Concat([typeof(TService)]), isComponentSetMember) {}

   internal ComponentRegistration<TService> CreatedBy<TImplementation>(Func<IServiceResolver, TImplementation> factoryMethod, IEnumerable<Type> dependencyTypes)
      where TImplementation : TService
   {
      var implementationType = typeof(TImplementation);
      AssertImplementsAllServices(implementationType);
      return BuildRegistration(InstantiationSpec.FromFactoryMethod(serviceResolver => factoryMethod(serviceResolver), implementationType), dependencyTypes);
   }

   private protected ComponentRegistration<TService> BuildRegistration(InstantiationSpec instantiationSpec, IEnumerable<Type> dependencyTypes)
   {
      var registration = new ComponentRegistration<TService>(Lifestyle,
                                                             ServiceTypes,
                                                             instantiationSpec,
                                                             dependencyTypes,
                                                             IsComponentSetMember,
                                                             SingletonDependentAllowed,
                                                             ScopedDependentAllowed,
                                                             ShouldDelegateToParentWhenCloning);
      AttachAssociatedRegistrations(registration);
      return registration;
   }

   private protected void AssertImplementsAllServices(Type implementationType)
   {
      var unImplementedService = ServiceTypes.FirstOrDefault(serviceType => !serviceType.IsAssignableFrom(implementationType));
      if(unImplementedService != null)
      {
         throw new ArgumentException($"{implementationType.FullName} does not implement: {unImplementedService.FullName}");
      }
   }
}
