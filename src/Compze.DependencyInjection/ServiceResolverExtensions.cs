using System.Reflection;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.DependencyInjection._private;

namespace Compze.DependencyInjection;

/// <summary>
/// Adds <c>WithServiceResolver()</c> — the supported way to break a constructor-injection cycle.
/// </summary>
/// <remarks>
/// Built entirely on the general associated-registrations extension point
/// (<see cref="ComponentRegistrationWithoutInstantiationSpecCE"/>'s deferred <c>WithAssociatedRegistrations()</c> modifier); the
/// core registration types have no knowledge of service resolvers. It lives here as an ordinary extension precisely so it is not a
/// special case baked into the core.
/// </remarks>
public static class ServiceResolverExtensions
{
   extension<TSpec>(TSpec @this) where TSpec : ComponentRegistrationWithoutInstantiationSpec
   {
      /// <summary>
      /// Also expose this component through an <see cref="IServiceResolver{TService}"/> for <em>each</em> of the service types it
      /// is registered for, so other components can depend on a deferred, typed resolver for it instead of the component itself —
      /// the supported way to break a constructor-injection cycle.
      /// </summary>
      /// <remarks>
      /// Each resolver is registered at this component's own <see cref="ComponentRegistration.Lifestyle"/>, and carries its
      /// <see cref="TransientRegistrationWithoutInstantiationSpec{TService}.AllowSingletonDependent"/> and
      /// <see cref="TransientRegistrationWithoutInstantiationSpec{TService}.AllowScopedDependent"/> opt-ins, so a dependency on a
      /// resolver is subject to exactly the same lifestyle validation as a direct dependency on the component would be.<br/>
      /// The depending side must call <see cref="IServiceResolver{TService}.Resolve"/> AFTER construction, never during it — resolving
      /// in the constructor re-forms the very cycle this breaks.
      /// </remarks>
      /// <remarks>
      /// Not available for a component set member (<c>ForSet(...)</c>): with many implementations sharing one service type, "the"
      /// resolver for that type has no single meaning.
      /// </remarks>
      public TSpec WithServiceResolver()
      {
         Contract.Argument.Assert(!@this.IsComponentSetMember,
            () => $"{nameof(WithServiceResolver)} is not supported for a component set member — its service type has no single instance for the resolver to defer to.");
         return @this.WithAssociatedRegistrations(builtRegistration => builtRegistration.ServiceTypes.Select(serviceType => CreateServiceResolverRegistration(serviceType, builtRegistration)));
      }
   }

   static ComponentRegistration CreateServiceResolverRegistration(Type serviceType, ComponentRegistration targetRegistration) =>
      (ComponentRegistration)CreateTypedServiceResolverRegistrationDefinition
         .MakeGenericMethod(serviceType)
         .Invoke(obj: null, parameters: [targetRegistration])!;

   static readonly MethodInfo CreateTypedServiceResolverRegistrationDefinition =
      typeof(ServiceResolverExtensions).GetMethod(nameof(CreateTypedServiceResolverRegistration), BindingFlags.NonPublic | BindingFlags.Static)!;

   static ComponentRegistration<IServiceResolver<TServiceType>> CreateTypedServiceResolverRegistration<TServiceType>(ComponentRegistration targetRegistration) where TServiceType : class =>
      new(targetRegistration.Lifestyle,
          serviceTypes: [typeof(IServiceResolver<TServiceType>)],
          InstantiationSpec.FromFactoryMethod(serviceResolver => new ServiceResolver<TServiceType>(serviceResolver), typeof(ServiceResolver<TServiceType>)),
          dependencyTypes: [],
          isComponentSetMember: false,
          allowSingletonDependent: targetRegistration.AllowSingletonDependent,
          allowScopedDependent: targetRegistration.AllowScopedDependent);
}
