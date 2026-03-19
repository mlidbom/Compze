using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection;

public abstract class ContainerBuilderBase : IContainerBuilder
{
   readonly List<ComponentRegistration> _registeredComponents = [];
   readonly IComponentRegistrar _registrar;
   bool _built;

   public bool IsClone { get; internal set; }

   protected ContainerBuilderBase(IComponentRegistrar? registrar)
   {
      _registrar = registrar ?? new ComponentRegistrar();
      ((ComponentRegistrar)_registrar).SetBuilder(this);
   }

   IComponentRegistrar IContainerBuilder.Registrar => _registrar;

   public IDependencyInjectionContainer Build()
   {
      if(_built) throw new InvalidOperationException("Build() has already been called on this builder. A builder can only be built once.");
      _built = true;
      return BuildContainer();
   }

   protected abstract BuiltContainerBase BuildContainer();

   internal void Register(params ComponentRegistration[] registrations)
   {
      if(_built) throw new InvalidOperationException("Cannot register components after the container has been built.");
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

   protected void AssertLifeStyleCombinationsAreValid()
   {
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
   }

   static bool IsInvalidLifestyleCombination(ComponentRegistration consumer, ComponentRegistration dependency)
   {
      if(consumer.Lifestyle == Lifestyle.Singleton)
         return dependency.Lifestyle switch
         {
            Lifestyle.Singleton => false,
            Lifestyle.Scoped => true,
            _ => !dependency.AllowSingletonDependent
         };

      if(consumer.Lifestyle == Lifestyle.Scoped)
         return dependency.Lifestyle is Lifestyle.TrackedTransient
                && !dependency.AllowScopedDependent;

      return false;
   }
}
