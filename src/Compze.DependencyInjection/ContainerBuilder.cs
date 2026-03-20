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

   IDependencyInjectionContainer IContainerBuilder.Build() => Build();

   public virtual DependencyInjectionContainer Build()
   {
      Contract.State.Assert(!_built, () => "Build() has already been called on this builder. A Container can only be built once.");
      _built = true;
      AssertLifeStyleCombinationsAreValid();
      return BuildInternal();
   }

   protected abstract DependencyInjectionContainer BuildInternal();

   internal void Register(params ComponentRegistration[] registrations)
   {
      Contract.State.Assert(!_built, () => "Cannot register components after the container has been built.");   
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      RegisterInContainer(registrations);
   }

   public IComponentRegistrar Registrar => _registrar;

   protected abstract void RegisterInContainer(ComponentRegistration[] registrations);

   public IReadOnlyList<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   void ValidateNoDuplicateRegistrations(ComponentRegistration[] newRegistrations)
   {
      foreach(var serviceType in newRegistrations.SelectMany(it => it.ServiceTypes))
      {
         _registeredComponents
           .Where(it => it.ServiceTypes.Contains(serviceType))
           .ForEach(_ => throw new InvalidOperationException($"Service type '{serviceType.FullName}' is already registered."));
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
