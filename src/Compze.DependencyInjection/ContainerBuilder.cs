using System.Reflection;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.DependencyInjection._private;

namespace Compze.DependencyInjection;

public abstract class ContainerBuilder : IContainerBuilder
{
   readonly List<ComponentRegistration> _registeredComponents = [];
   readonly IComponentRegistrar _registrar;
   bool _built;

   internal bool IsClone { get; set; }

   protected ContainerBuilder(IComponentRegistrar? registrar)
   {
      _registrar = registrar ?? new ComponentRegistrar();
      ((ComponentRegistrar)_registrar).SetBuilder(this);
   }

   IComponentRegistrar IContainerBuilder.Registrar => _registrar;

   IDependencyInjectionContainer IContainerBuilder.Build(ContainerOptions? options) => Build(options);

   public virtual DependencyInjectionContainer Build(ContainerOptions? options = null)
   {
      Contract.State.Assert(!_built, () => "Build() has already been called on this builder. A Container can only be built once.");
      _built = true;
      Options = options ?? ContainerOptions.Default;
      AddAssociatedRegistrations();
      AddComponentSetRegistrations();
      AssertLifeStyleCombinationsAreValid();
      AssertNoSingularDependenciesOnComponentSetTypes();
      RegisterInContainer(_registeredComponents.ToArray());
      return BuildInternal();
   }

   void AddAssociatedRegistrations()
   {
      var associatedRegistrations = CollectAssociatedRegistrationsRecursively();
      if(associatedRegistrations.Length == 0) return;
      AssertNoHandWrittenComponentSetInjectionRegistrations(associatedRegistrations);
      ValidateNoDuplicateRegistrations(associatedRegistrations);
      _registeredComponents.AddRange(associatedRegistrations);
      return;

      ComponentRegistration[] CollectAssociatedRegistrationsRecursively()
      {
         var collected = new List<ComponentRegistration>();
         var alreadyIncluded = _registeredComponents.ToHashSet();
         var toExpand = new Queue<ComponentRegistration>(_registeredComponents);
         while(toExpand.TryDequeue(out var registration))
         {
            foreach(var associated in registration.AssociatedRegistrations)
            {
               if(!alreadyIncluded.Add(associated)) continue;
               collected.Add(associated);
               toExpand.Enqueue(associated);
            }
         }

         return [..collected];
      }
   }

   protected ContainerOptions Options { get; set; } = ContainerOptions.Default;

   protected abstract DependencyInjectionContainer BuildInternal();

   internal void Register(params ComponentRegistration[] registrations)
   {
      Contract.State.Assert(!_built, () => "Cannot register components after the container has been built.");
      AssertNoHandWrittenComponentSetInjectionRegistrations(registrations);
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
   }

   public IComponentRegistrar Registrar => _registrar;

   protected abstract void RegisterInContainer(ComponentRegistration[] registrations);

   public IReadOnlyList<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   void ValidateNoDuplicateRegistrations(ComponentRegistration[] newRegistrations)
   {
      // Adding the new registrations' service types to the sets as we check catches duplicates within the new batch itself, not just against what is already registered.
      var singularServiceTypes = _registeredComponents.Where(it => !it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      var componentSetServiceTypes = _registeredComponents.Where(it => it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();

      foreach(var registration in newRegistrations)
      {
         foreach(var serviceType in registration.ServiceTypes)
         {
            if(registration.IsComponentSetMember)
            {
               if(singularServiceTypes.Contains(serviceType))
                  throw new InvalidOperationException($"Service type '{serviceType.FullName}' is already registered as a singular service and cannot also be registered as a component set member.");

               // Multiple registrations sharing a component set's service type is the entire point of a component set — no duplicate check here.
               componentSetServiceTypes.Add(serviceType);
            } else
            {
               if(componentSetServiceTypes.Contains(serviceType))
                  throw new InvalidOperationException($"Service type '{serviceType.FullName}' is already registered as a component set member and cannot also be registered as a singular service.");

               if(!singularServiceTypes.Add(serviceType))
                  throw new InvalidOperationException($"Service type '{serviceType.FullName}' is already registered.");
            }
         }
      }
   }

   void AssertLifeStyleCombinationsAreValid() =>
      _registeredComponents.ForEach(consumer =>
      {
         foreach(var dependencyType in consumer.DependencyTypes)
         {
            _registeredComponents
              .Where(it => it.ProvidesService(dependencyType))
              .Where(dependency => IsInvalidLifestyleCombination(consumer, dependency))
              .ForEach(invalidDependency => throw new InvalidLifeStyleCombinationException(consumer, invalidDependency, dependencyType));
         }
      });

   /// <summary>
   /// A <c>CreatedBy(...)</c> constructor dependency is always resolved singularly (see the generated overloads in
   /// <see cref="ComponentRegistrationExtensions"/>), so depending on a component set's service type there could never mean what
   /// it looks like — it would silently bind to whichever set member the container happens to resolve first, not the whole set.
   /// </summary>
   void AssertNoSingularDependenciesOnComponentSetTypes()
   {
      var componentSetServiceTypes = _registeredComponents.Where(it => it.IsComponentSetMember).SelectMany(it => it.ServiceTypes).ToHashSet();
      _registeredComponents.ForEach(consumer =>
         consumer.DependencyTypes
                 .Where(componentSetServiceTypes.Contains)
                 .ForEach(dependencyType => throw new InvalidOperationException(
                    $"{consumer.InstantiationSpec.FactoryMethodReturnType.FullName} depends on '{dependencyType.FullName}' via CreatedBy(...), "
                    + $"but '{dependencyType.FullName}' is registered as a component set member — depend on IComponentSet<{dependencyType.Name}> to receive the whole set.")));
   }

   /// <summary>
   /// Synthesizes the singular <see cref="IComponentSet{TService}"/> registration for each component-set service type, so
   /// components can take the whole set as an ordinary <c>CreatedBy(...)</c> constructor dependency. The set's lifestyle follows
   /// its members' — <see cref="Lifestyle.Singleton"/> when every member is a singleton, <see cref="Lifestyle.Scoped"/>
   /// otherwise — so lifestyle validation guards a dependency on the set exactly as it would a dependency on the members.
   /// A set nothing contributed to is still a set — the empty one: a <c>CreatedBy(...)</c> dependency on an
   /// <see cref="IComponentSet{TService}"/> with no <c>ForSet(...)</c> members receives the empty set (synthesized as a
   /// singleton — with no members there is nothing whose lifestyle could vary), because zero contributions is a legitimate
   /// state for a contribution seam, not a wiring error.
   /// </summary>
   /// <remarks>
   /// Skips a set whose <see cref="IComponentSet{TService}"/> is already among the registrations: a clone or child container
   /// builder receives the source container's synthesized registration — delegated or copied like any other of its lifestyle —
   /// and must not synthesize a duplicate.
   /// </remarks>
   void AddComponentSetRegistrations()
   {
      var alreadyProvidedSetInjectionTypes = _registeredComponents.SelectMany(it => it.ServiceTypes).Where(IsComponentSetInjectionType).ToHashSet();

      _registeredComponents.Where(it => it.IsComponentSetMember)
                           .GroupBy(it => it.ServiceTypes.Single())
                           .Where(setMembers => !alreadyProvidedSetInjectionTypes.Contains(typeof(IComponentSet<>).MakeGenericType(setMembers.Key)))
                           .Select(setMembers => CreateComponentSetRegistration(
                                      setMembers.Key,
                                      setMembers.All(member => member.Lifestyle == Lifestyle.Singleton) ? Lifestyle.Singleton : Lifestyle.Scoped))
                           .ToList()
                           .ForEach(_registeredComponents.Add);

      var providedSetInjectionTypes = _registeredComponents.SelectMany(it => it.ServiceTypes).Where(IsComponentSetInjectionType).ToHashSet();
      _registeredComponents.SelectMany(it => it.DependencyTypes)
                           .Where(IsComponentSetInjectionType)
                           .Distinct()
                           .Where(dependedOnSetInjectionType => !providedSetInjectionTypes.Contains(dependedOnSetInjectionType))
                           .Select(dependedOnSetInjectionType => CreateComponentSetRegistration(dependedOnSetInjectionType.GenericTypeArguments[0], Lifestyle.Singleton))
                           .ToList()
                           .ForEach(_registeredComponents.Add);
   }

   /// <summary>
   /// <see cref="IComponentSet{TService}"/> registrations are the container's own vocabulary: a hand-written one would silently
   /// decouple what <c>CreatedBy(...)</c> consumers receive from the set's actual <c>ForSet(...)</c> members. Only the
   /// container-synthesized registrations — recognizable by their internal <see cref="ComponentSet{TService}"/> implementation
   /// type, which nothing else can construct — may provide the type; a clone or child container builder re-registering its
   /// source's synthesized registration passes for exactly that reason.
   /// </summary>
   static void AssertNoHandWrittenComponentSetInjectionRegistrations(ComponentRegistration[] registrations) =>
      registrations.Where(registration => !IsContainerSynthesizedComponentSetRegistration(registration))
                   .SelectMany(registration => registration.ServiceTypes)
                   .Where(IsComponentSetInjectionType)
                   .ForEach(serviceType => throw new InvalidOperationException(
                      $"'{serviceType.FullName}' cannot be registered: the container synthesizes the IComponentSet registration from the set's ForSet(...) members — register members and depend on IComponentSet<{serviceType.GenericTypeArguments[0].Name}>."));

   static bool IsComponentSetInjectionType(Type serviceType) => serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IComponentSet<>);

   static bool IsContainerSynthesizedComponentSetRegistration(ComponentRegistration registration) =>
      registration.InstantiationSpec.FactoryMethodReturnType is { IsGenericType: true } implementationType && implementationType.GetGenericTypeDefinition() == typeof(ComponentSet<>);

   static ComponentRegistration CreateComponentSetRegistration(Type serviceType, Lifestyle lifestyle) =>
      (ComponentRegistration)CreateTypedComponentSetRegistrationDefinition.MakeGenericMethod(serviceType).Invoke(obj: null, parameters: [lifestyle])!;

   static readonly MethodInfo CreateTypedComponentSetRegistrationDefinition =
      typeof(ContainerBuilder).GetMethod(nameof(CreateTypedComponentSetRegistration), BindingFlags.NonPublic | BindingFlags.Static)!;

   static ComponentRegistration<IComponentSet<TService>> CreateTypedComponentSetRegistration<TService>(Lifestyle lifestyle) where TService : class =>
      new(lifestyle,
          serviceTypes: [typeof(IComponentSet<TService>)],
          InstantiationSpec.FromFactoryMethod(serviceResolver => new ComponentSet<TService>(serviceResolver), typeof(ComponentSet<TService>)),
          dependencyTypes: [],
          isComponentSetMember: false);

   static bool IsInvalidLifestyleCombination(ComponentRegistration consumer, ComponentRegistration dependency)
   {
      if(consumer.Lifestyle == Lifestyle.Singleton)
         return dependency.Lifestyle switch
         {
            Lifestyle.Singleton => false,
            Lifestyle.Scoped    => true,
            _                   => !dependency.AllowSingletonDependent
         };

      if(consumer.Lifestyle == Lifestyle.Scoped)
         return dependency.Lifestyle is Lifestyle.TrackedTransient
             && !dependency.AllowScopedDependent;

      return false;
   }
}
