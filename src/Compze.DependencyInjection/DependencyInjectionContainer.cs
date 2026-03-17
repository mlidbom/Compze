using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.LinqCE;

namespace Compze.DependencyInjection;

public abstract partial class DependencyInjectionContainer : IDependencyInjectionContainer
{
   static readonly ILogger Log = CompzeLogger.For<DependencyInjectionContainer>();

   readonly List<ComponentRegistration> _registeredComponents = [];
   readonly IComponentRegistrar _registrar;

   public bool IsClone { get; private set; }

   protected DependencyInjectionContainer(IComponentRegistrar? registrar)
   {
      _registrar = registrar ?? new ComponentRegistrar();
      _registrar.SetContainer(this);
   }

   public abstract void Dispose();
   public abstract ValueTask DisposeAsync();

   protected virtual IReadOnlyList<Type> ContainerFacadeServiceTypes { get; } =
      [typeof(IDependencyInjectionContainer), typeof(IServiceLocator)];

   protected abstract DependencyInjectionContainer CreateEmptyClone();

   public IDependencyInjectionContainer Clone()
   {
      Log.Info($"Cloning IDependencyInjectionContainer: {GetHashCode()}");
      var sourceServiceLocator = ServiceLocator;
      var cloneContainer = CreateEmptyClone();
      cloneContainer.IsClone = true;

      cloneContainer.Register(Singleton.For<IServiceLocator>().CreatedBy(() => cloneContainer.ServiceLocator));

      RegisteredComponents()
        .Where(component => ContainerFacadeServiceTypes.None(facadeType => component.ServiceTypes.Contains(facadeType)))
        .ForEach(action: registration => cloneContainer.Register(registration.CreateCloneRegistration(sourceServiceLocator)));

      return cloneContainer;
   }

   public IDependencyInjectionContainer Register(params ComponentRegistration[] registrations)
   {
      ValidateNoDuplicateRegistrations(registrations);
      _registeredComponents.AddRange(registrations);
      RegisterInContainer(registrations);
      return this;
   }

   public IComponentRegistrar Register() => _registrar;

   protected abstract IDependencyInjectionContainer RegisterInContainer(ComponentRegistration[] registrations);

   public IEnumerable<ComponentRegistration> RegisteredComponents() => _registeredComponents;

   public abstract IServiceLocator ServiceLocator { get; }

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