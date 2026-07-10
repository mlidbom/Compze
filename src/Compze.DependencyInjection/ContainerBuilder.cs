using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;

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
      AssertLifeStyleCombinationsAreValid();
      RegisterInContainer(_registeredComponents.ToArray());
      return BuildInternal();
   }

   void AddAssociatedRegistrations()
   {
      var associatedRegistrations = CollectAssociatedRegistrationsRecursively();
      if(associatedRegistrations.Length == 0) return;
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
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
   }

   public IComponentRegistrar Registrar => _registrar;

   protected abstract void RegisterInContainer(ComponentRegistration[] registrations);

   public IReadOnlyList<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   void ValidateNoDuplicateRegistrations(ComponentRegistration[] newRegistrations)
   {
      // Adding the new registrations' service types to the set as we check catches duplicates within the new batch itself, not just against what is already registered.
      var registeredServiceTypes = _registeredComponents.SelectMany(it => it.ServiceTypes).ToHashSet();
      foreach(var serviceType in newRegistrations.SelectMany(it => it.ServiceTypes))
      {
         if(!registeredServiceTypes.Add(serviceType))
            throw new InvalidOperationException($"Service type '{serviceType.FullName}' is already registered.");
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
